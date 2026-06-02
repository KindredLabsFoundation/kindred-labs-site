document.addEventListener('DOMContentLoaded', function () {
    const form = document.getElementById('mainForm');
    if (!form) return;

    const editMode = document.getElementById('edit-mode');
    const previewMode = document.getElementById('preview-mode');
    const previewContent = document.getElementById('preview-content');
    const signaturePadElement = document.getElementById('signature-pad');
    const signatureInput = document.getElementById('signature-input');
    const saveStatus = document.getElementById('save-status');

    // Get configuration from data attributes
    const layersCount = parseInt(form.dataset.layersCount || "0");
    const draftSavedAtTemplate = form.dataset.draftSavedAtTemplate || "Saved at {0}";
    const scoreLabel = form.dataset.scoreLabel || "Score";
    const signatureRequiredMsg = form.dataset.signatureRequiredMsg || "Signature is required";

    let signaturePad;
    if (signaturePadElement) {
        signaturePad = new SignaturePad(signaturePadElement);

        // Resize canvas
        function resizeCanvas() {
            const ratio = Math.max(window.devicePixelRatio || 1, 1);
            signaturePadElement.width = signaturePadElement.offsetWidth * ratio;
            signaturePadElement.height = signaturePadElement.offsetHeight * ratio;
            signaturePadElement.getContext("2d").scale(ratio, ratio);
            signaturePad.clear();
        }
        window.addEventListener("resize", resizeCanvas);
        resizeCanvas();
    }

    function resetFormUI() {
        document.querySelectorAll('.domain-card').forEach(card => {
            const isNA = card.querySelector('.not-applicable-checkbox')?.checked;
            const levelRows = card.querySelectorAll('.level-check-row');
            
            if (isNA) {
                levelRows.forEach(row => row.classList.add('hidden'));
                const naDoc = card.querySelector('.na-documentation-container');
                if (naDoc) naDoc.classList.remove('hidden');
            } else {
                const naDoc = card.querySelector('.na-documentation-container');
                if (naDoc) naDoc.classList.add('hidden');
                
                // Explicitly check DOM state for each level
                levelRows.forEach(row => {
                    const level = parseInt(row.dataset.level);
                    if (level === 0) {
                        row.classList.remove('hidden');
                    } else {
                        const prevLevelRow = card.querySelector(`.level-check-row[data-level="${level - 1}"]`);
                        const prevLevelYes = prevLevelRow?.querySelector('input[value="true"]')?.checked;
                        
                        if (prevLevelYes) {
                            row.classList.remove('hidden');
                        } else {
                            row.classList.add('hidden');
                        }
                    }
                });
            }

            // Reset domain scores
            const scoreBadge = card.querySelector('.domain-score-badge');
            const scoreValue = card.querySelector('.domain-score-value');
            const scoreInput = card.querySelector('.domain-score-input');

            if (scoreBadge) scoreBadge.classList.remove('hidden');
            if (scoreValue) scoreValue.textContent = isNA ? 'N/A' : '0';
            if (scoreInput) scoreInput.value = isNA ? 'N/A' : '0';
            
            // Sync summary table
            const domainId = card.dataset.domainId;
            const summaryScores = document.querySelectorAll(`.summary-domain-score[data-domain-id="${domainId}"]`);
            summaryScores.forEach(el => {
                el.textContent = isNA ? 'N/A' : '0';
            });
        });

        // Reset Layer scores
        document.querySelectorAll('.layer-score').forEach(layerScoreSpan => {
            const lIdx = layerScoreSpan.dataset.layerIndex;
            const layerScoreInputs = document.querySelectorAll(`.layer-score-input[name$="Layers[${lIdx}].LayerScore"]`);
            const summaryLayerScores = document.querySelectorAll(`.summary-layer-score[data-layer-index="${lIdx}"]`);
            
            layerScoreSpan.textContent = '0';
            layerScoreInputs.forEach(i => i.value = '0');
            summaryLayerScores.forEach(s => s.textContent = '0');
        });
    }

    function calculateScores() {
        document.querySelectorAll('.domain-card').forEach(card => {
            const lIdx = card.dataset.layerIndex;
            const dIdx = card.dataset.domainIndex;
            const isNA = card.querySelector('.not-applicable-checkbox')?.checked;
            
            const scoreBadge = card.querySelector('.domain-score-badge');
            const scoreValue = card.querySelector('.domain-score-value');
            const scoreInput = card.querySelector('.domain-score-input');
            const levelRows = card.querySelectorAll('.level-check-row');
            
            if (isNA) {
                scoreBadge.classList.remove('hidden');
                scoreValue.textContent = 'N/A';
                scoreInput.value = 'N/A';
                levelRows.forEach(row => {
                    row.classList.add('opacity-40', 'pointer-events-none', 'hidden');
                });
                const naDoc = card.querySelector('.na-documentation-container');
                if (naDoc) naDoc.classList.remove('hidden');
            } else {
                const naDoc = card.querySelector('.na-documentation-container');
                if (naDoc) naDoc.classList.add('hidden');
                let domainScore = 0;
                let reachedFirstNo = false;

                levelRows.forEach(row => {
                    const level = parseInt(row.dataset.level);
                    const yesRadio = row.querySelector('input[value="true"]');
                    const noRadio = row.querySelector('input[value="false"]');
                    
                    // Level Locking: Show/Hide instead of Enable/Disable
                    if (level > 0) {
                        const prevLevelRow = card.querySelector(`.level-check-row[data-level="${level-1}"]`);
                        const prevLevelYes = prevLevelRow.querySelector('input[value="true"]').checked;
                        if (!prevLevelYes) {
                            row.classList.add('hidden');
                            // Reset subsequent levels if not answered
                            if (!yesRadio.checked && !noRadio.checked) {
                                // already unanswered
                            } else if (!prevLevelYes) {
                                // Reset if previous became No or unselected
                                yesRadio.checked = false;
                                noRadio.checked = false;
                            }
                        } else {
                            row.classList.remove('hidden');
                        }
                    } else {
                        // Level 0 is always visible
                        row.classList.remove('hidden');
                    }

                    if (yesRadio.checked) domainScore = level;
                    if (noRadio.checked) reachedFirstNo = true;
                });

                const level3Row = card.querySelector('.level-check-row[data-level="3"]');
                const level3Yes = level3Row && level3Row.querySelector('input[value="true"]').checked;
                
                scoreBadge.classList.remove('hidden');
                scoreValue.textContent = domainScore;
                scoreInput.value = domainScore;
            }

            // Update summary table domain scores
            const domainId = card.dataset.domainId;
            const summaryScore = document.querySelectorAll(`.summary-domain-score[data-domain-id="${domainId}"]`);
            summaryScore.forEach(el => {
                el.textContent = isNA ? 'N/A' : (scoreInput.value || '0');
            });
        });

        // Calculate Layer Scores
        document.querySelectorAll('.layer-score').forEach(layerScoreSpan => {
            const lIdx = layerScoreSpan.dataset.layerIndex;
            const domainCards = document.querySelectorAll(`.domain-card[data-layer-index="${lIdx}"]`);
            
            let totalScore = 0;
            let applicableCount = 0;

            domainCards.forEach(card => {
                const isNA = card.querySelector('.not-applicable-checkbox')?.checked;
                if (!isNA) {
                    applicableCount++;
                    const val = card.querySelector('.domain-score-input').value;
                    if (val !== '' && val !== 'N/A') {
                        totalScore += parseInt(val);
                    }
                }
            });

            const layerScoreInputs = document.querySelectorAll(`.layer-score-input[name$="Layers[${lIdx}].LayerScore"]`);
            const summaryLayerScores = document.querySelectorAll(`.summary-layer-score[data-layer-index="${lIdx}"]`);

            if (applicableCount > 0) {
                const score = Math.floor(totalScore / applicableCount);
                layerScoreSpan.textContent = score;
                layerScoreInputs.forEach(i => i.value = score);
                summaryLayerScores.forEach(s => s.textContent = score);
            } else {
                layerScoreSpan.textContent = 'N/A';
                layerScoreInputs.forEach(i => i.value = 'N/A');
                summaryLayerScores.forEach(s => s.textContent = 'N/A');
            }
        });
    }

    // Sync Assessor Role to sign-off
    const assessorRoleInput = document.querySelector('input[name="Input.AssessorRole"]');
    
    function syncAssessorRole() {
        if (assessorRoleInput) {
            const roleDisplays = document.querySelectorAll('.sign-off-role-display');
            roleDisplays.forEach(d => {
                d.textContent = assessorRoleInput.value;
            });
        }
    }
    
    if (assessorRoleInput) {
        assessorRoleInput.addEventListener('input', syncAssessorRole);
        syncAssessorRole();
    }

    form.addEventListener('change', calculateScores);
    
    // Initial state
    resetFormUI();
    calculateScores();

    form.addEventListener('beforeFormClear', () => {
        document.querySelectorAll('.domain-card').forEach(card => {
            const level0Row = card.querySelector('.level-check-row[data-level="0"]');
            const noRadio = level0Row?.querySelector('input[value="false"]');
            if (noRadio) {
                noRadio.click();
            }
        });
    });

    // Auto-save
    let isDirty = false;
    form.querySelectorAll('input, select, textarea').forEach(el => {
        el.addEventListener('input', () => isDirty = true);
        el.addEventListener('change', () => isDirty = true);
    });

    setInterval(() => {
        if (isDirty) {
            saveDraft();
        }
    }, 60000);

    async function saveDraft() {
        const formData = new FormData(form);
        const data = {
            RecordId: formData.get('Input.RecordId'),
            Date: formData.get('Input.Date'),
            FrameworkVersion: formData.get('Input.FrameworkVersion'),
            OrganizationName: formData.get('Input.OrganizationName'),
            AssessorName: formData.get('Input.AssessorName'),
            AssessorRole: formData.get('Input.AssessorRole'),
            AssessorOrganization: formData.get('Input.AssessorOrganization'),
            AssessmentType: formData.get('Input.AssessmentType'),
            Layer1GapNarrative: formData.get('Input.Layer1GapNarrative'),
            Layer2GapNarrative: formData.get('Input.Layer2GapNarrative'),
            Layer3GapNarrative: formData.get('Input.Layer3GapNarrative'),
            Layers: []
        };

        // Extract Layers
        for (let l = 0; l < layersCount; l++) {
            const layer = {
                Name: formData.get(`Input.Layers[${l}].Name`),
                LayerScore: formData.get(`Input.Layers[${l}].LayerScore`),
                Domains: []
            };
            
            const domainCards = document.querySelectorAll(`.domain-card[data-layer-index="${l}"]`);
            domainCards.forEach((card, d) => {
                const domain = {
                    Id: formData.get(`Input.Layers[${l}].Domains[${d}].Id`),
                    Name: formData.get(`Input.Layers[${l}].Domains[${d}].Name`),
                    IsNotApplicable: card.querySelector('.not-applicable-checkbox')?.checked || false,
                    NaDocumentation: formData.get(`Input.Layers[${l}].Domains[${d}].NaDocumentation`),
                    DomainScore: card.querySelector('.domain-score-input').value,
                    Levels: []
                };
                
                const levelRows = card.querySelectorAll('.level-check-row');
                levelRows.forEach((row, lv) => {
                    const yes = row.querySelector('input[value="true"]').checked;
                    const no = row.querySelector('input[value="false"]').checked;
                    domain.Levels.push({
                        Level: lv,
                        Answer: yes ? true : (no ? false : null)
                    });
                });
                layer.Domains.push(domain);
            });
            data.Layers.push(layer);
        }

        try {
            const response = await fetch('?handler=SaveDraft', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify(data)
            });
            const result = await response.json();
            if (result.success) {
                saveStatus.textContent = draftSavedAtTemplate.replace('{0}', result.lastSaved);
                isDirty = false;
            }
        } catch (error) {
            console.error('Auto-save failed:', error);
        }
    }

    // Preview
    const previewBtn = document.getElementById('preview-btn');
    if (previewBtn) {
        previewBtn.addEventListener('click', () => {
            const formData = new FormData(form);
            let html = '<div class="space-y-12">';
            
            // Labels from data attributes
            const recordIdLabel = form.dataset.recordIdLabel || "Record ID";
            const assessmentDateLabel = form.dataset.assessmentDateLabel || "Assessment Date";
            const organizationNameLabel = form.dataset.organizationNameLabel || "Organization Name";
            const assessmentTypeLabel = form.dataset.assessmentTypeLabel || "Assessment Type";
            const assessorNameLabel = form.dataset.assessorNameLabel || "Assessor Name";
            const assessorRoleLabel = form.dataset.assessorRoleLabel || "Assessor Role";
            const notApplicableLabel = form.dataset.notApplicableLabel || "Not Applicable";
            const naDocumentationLabel = form.dataset.naDocumentationLabel || "N/A Documentation";
            const layer1GapNarrativeLabel = form.dataset.layer1GapNarrativeLabel || "Layer 1 Gap Narrative";
            const layer2GapNarrativeLabel = form.dataset.layer2GapNarrativeLabel || "Layer 2 Gap Narrative";
            const layer3GapNarrativeLabel = form.dataset.layer3GapNarrativeLabel || "Layer 3 Gap Narrative";

            // Header info
            html += `
                <div class="grid grid-cols-1 md:grid-cols-2 gap-x-12 gap-y-4 text-sm pb-8 border-b border-white/10">
                    <div><span class="opacity-60 uppercase text-[10px] block">${recordIdLabel}</span><span class="font-bold">${formData.get('Input.RecordId') || ''}</span></div>
                    <div><span class="opacity-60 uppercase text-[10px] block">${assessmentDateLabel}</span><span class="font-bold">${formData.get('Input.Date') || ''}</span></div>
                    <div><span class="opacity-60 uppercase text-[10px] block">${organizationNameLabel}</span><span class="font-bold">${formData.get('Input.OrganizationName') || ''}</span></div>
                    <div><span class="opacity-60 uppercase text-[10px] block">${assessmentTypeLabel}</span><span class="font-bold">${formData.get('Input.AssessmentType') || ''}</span></div>
                    <div><span class="opacity-60 uppercase text-[10px] block">${assessorNameLabel}</span><span class="font-bold">${formData.get('Input.AssessorName') || ''}</span></div>
                    <div><span class="opacity-60 uppercase text-[10px] block">${assessorRoleLabel}</span><span class="font-bold">${formData.get('Input.AssessorRole') || ''}</span></div>
                </div>
            `;

            // Layers and Domains
            document.querySelectorAll('.domain-card').forEach(card => {
                const domainId = card.dataset.domainId;
                const domainName = card.querySelector('h3').textContent;
                const isNA = card.querySelector('.not-applicable-checkbox')?.checked;
                const score = card.querySelector('.domain-score-input').value;
                
                html += `<div class="preview-domain">
                    <div class="flex justify-between items-center mb-4">
                        <h3 class="text-kindred-gold font-bold">${domainName}</h3>
                        <div class="px-2 py-1 bg-white/10 rounded border border-white/10 text-xs font-bold">
                            ${scoreLabel}: ${isNA ? 'N/A' : (score || '—')}
                        </div>
                    </div>`;
                
                if (isNA) {
                    html += `<div class="italic opacity-60 text-sm mb-4">${notApplicableLabel}</div>`;
                    html += `<div class="text-sm bg-white/5 p-3 rounded mb-4"><span class="block text-[10px] uppercase opacity-40 mb-1">${naDocumentationLabel}</span>${formData.get(card.querySelector('textarea').name) || '—'}</div>`;
                } else {
                    html += `<div class="grid grid-cols-1 gap-2">`;
                    card.querySelectorAll('.level-check-row').forEach(row => {
                        const text = row.querySelector('.flex-1').textContent;
                        const yes = row.querySelector('input[value="true"]').checked;
                        const no = row.querySelector('input[value="false"]').checked;
                        const answer = yes ? '<span class="text-green-400 font-bold">YES</span>' : (no ? '<span class="text-red-400 font-bold">NO</span>' : '<span class="opacity-20">—</span>');
                        
                        html += `<div class="flex justify-between text-sm py-1 border-b border-white/5">
                            <span>${text}</span>
                            <span class="ml-4">${answer}</span>
                        </div>`;
                    });
                    html += `</div>`;
                }
                html += `</div>`;
            });

            // Summary Table
            const summaryTableContainer = document.querySelector('.overflow-x-auto');
            if (summaryTableContainer) {
                const summaryTable = summaryTableContainer.cloneNode(true);
                html += '<div class="pt-8 border-t border-white/10">' + summaryTable.innerHTML + '</div>';
            }
            // Narratives
            html += `
                <div class="space-y-6 pt-8 border-t border-white/10">
                    <div>
                        <span class="block text-kindred-gold font-bold mb-2">${layer1GapNarrativeLabel}</span>
                        <div class="bg-white/5 p-4 rounded-lg text-sm whitespace-pre-wrap">${formData.get('Input.Layer1GapNarrative') || '—'}</div>
                    </div>
                    <div>
                        <span class="block text-kindred-gold font-bold mb-2">${layer2GapNarrativeLabel}</span>
                        <div class="bg-white/5 p-4 rounded-lg text-sm whitespace-pre-wrap">${formData.get('Input.Layer2GapNarrative') || '—'}</div>
                    </div>
                    <div>
                        <span class="block text-kindred-gold font-bold mb-2">${layer3GapNarrativeLabel}</span>
                        <div class="bg-white/5 p-4 rounded-lg text-sm whitespace-pre-wrap">${formData.get('Input.Layer3GapNarrative') || '—'}</div>
                    </div>
                </div>
            `;

            html += '</div>';
            previewContent.innerHTML = html;
            
            const previewAssessorRole = document.getElementById('preview-assessor-role');
            if (previewAssessorRole) {
                previewAssessorRole.textContent = formData.get('Input.AssessorRole');
            }

            editMode.classList.add('hidden');
            previewMode.classList.remove('hidden');
            window.scrollTo(0, 0);
            if (typeof resizeCanvas === 'function') resizeCanvas();
        });
    }

    const backToEditBtn = document.getElementById('back-to-edit');
    if (backToEditBtn) {
        backToEditBtn.addEventListener('click', () => {
            previewMode.classList.add('hidden');
            editMode.classList.remove('hidden');
            window.scrollTo(0, 0);
        });
    }

    const clearSignatureBtn = document.getElementById('clear-signature');
    if (clearSignatureBtn) {
        clearSignatureBtn.addEventListener('click', () => {
            if (signaturePad) signaturePad.clear();
        });
    }

    const submitBtn = document.getElementById('submit-btn');
    if (submitBtn) {
        submitBtn.addEventListener('click', () => {
            if (signaturePad && signaturePad.isEmpty()) {
                alert(signatureRequiredMsg);
                return;
            }
            if (signaturePad) signatureInput.value = signaturePad.toDataURL();
            
            // Submit via hidden action input
            const actionInput = document.createElement('input');
            actionInput.type = 'hidden';
            actionInput.name = 'action';
            actionInput.value = 'submit';
            form.appendChild(actionInput);
            form.submit();
        });
    }
});
