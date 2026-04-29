using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using KindredLabs.Core.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace KindredLabs.Web.Services;

/// <summary>
/// Implementation of <see cref="IGitHubDiscussionsService"/> using the GitHub GraphQL API.
/// </summary>
public class GitHubDiscussionsService : IGitHubDiscussionsService
{
    private readonly HttpClient _httpClient;
    private readonly string _repositoryOwner;
    private readonly string _repositoryName;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubDiscussionsService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client configured for GitHub API.</param>
    /// <param name="configuration">The application configuration.</param>
    public GitHubDiscussionsService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        var token =
            configuration["GitHub:ApiToken"]
            ?? throw new InvalidOperationException("GitHub API token is not configured.");

        _repositoryOwner = configuration["GitHub:RepositoryOwner"] ?? "KindredLabsFoundation";
        _repositoryName = configuration["GitHub:RepositoryName"] ?? "upstream-governance-framework";

        _httpClient.BaseAddress = new Uri("https://api.github.com/graphql");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token
        );
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("KindredLabs-Site");
    }

    /// <inheritdoc />
    public async Task<IEnumerable<GitHubDiscussion>> GetDiscussionsByCategoryAsync(
        string categorySlug
    )
    {
        var query =
            @"
        query($owner: String!, $name: String!, $categorySlug: String!) {
          repository(owner: $owner, name: $name) {
            discussions(first: 100, categoryId: null) { # Note: categoryId filtering is tricky in GraphQL without knowing ID, 
                                                        # so we filter by slug in the result or use category query
            }
          }
        }";

        // Better query: Get category ID first or use category field if available
        var queryWithCategory =
            @"
        query($owner: String!, $name: String!, $categorySlug: String!) {
          repository(owner: $owner, name: $name) {
            discussionCategories(first: 10) {
              nodes {
                id
                name
                slug
              }
            }
          }
        }";

        var categoryResponse = await _httpClient.PostAsJsonAsync(
            "",
            new
            {
                query = queryWithCategory,
                variables = new
                {
                    owner = _repositoryOwner,
                    name = _repositoryName,
                    categorySlug,
                },
            }
        );

        categoryResponse.EnsureSuccessStatusCode();
        var categoryData = await categoryResponse.Content.ReadFromJsonAsync<JsonElement>();
        var categories = categoryData
            .GetProperty("data")
            .GetProperty("repository")
            .GetProperty("discussionCategories")
            .GetProperty("nodes");

        var targetCategory = categories
            .EnumerateArray()
            .FirstOrDefault(c => c.GetProperty("slug").GetString() == categorySlug);

        if (targetCategory.ValueKind == JsonValueKind.Undefined)
        {
            return Enumerable.Empty<GitHubDiscussion>();
        }

        var categoryId = targetCategory.GetProperty("id").GetString();

        var discussionsQuery =
            @"
        query($owner: String!, $name: String!, $categoryId: ID!) {
          repository(owner: $owner, name: $name) {
            discussions(first: 100, categoryId: $categoryId, orderBy: {field: CREATED_AT, direction: DESC}) {
              nodes {
                id
                title
                body
                url
                createdAt
                comments {
                  totalCount
                }
                category {
                  name
                }
              }
            }
          }
        }";

        var discussionsResponse = await _httpClient.PostAsJsonAsync(
            "",
            new
            {
                query = discussionsQuery,
                variables = new
                {
                    owner = _repositoryOwner,
                    name = _repositoryName,
                    categoryId,
                },
            }
        );

        discussionsResponse.EnsureSuccessStatusCode();
        var discussionsData = await discussionsResponse.Content.ReadFromJsonAsync<JsonElement>();
        var nodes = discussionsData
            .GetProperty("data")
            .GetProperty("repository")
            .GetProperty("discussions")
            .GetProperty("nodes");

        return nodes
            .EnumerateArray()
            .Select(n => new GitHubDiscussion(
                n.GetProperty("id").GetString() ?? string.Empty,
                n.GetProperty("title").GetString() ?? string.Empty,
                n.GetProperty("body").GetString() ?? string.Empty,
                n.GetProperty("url").GetString() ?? string.Empty,
                n.GetProperty("category").GetProperty("name").GetString() ?? string.Empty,
                n.GetProperty("comments").GetProperty("totalCount").GetInt32(),
                n.GetProperty("createdAt").GetDateTime()
            ));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<GitHubComment>> GetCommentsForDiscussionAsync(string discussionId)
    {
        var query =
            @"
        query($id: ID!) {
          node(id: $id) {
            ... on Discussion {
              comments(first: 100) {
                nodes {
                  id
                  body
                  createdAt
                  author {
                    login
                  }
                }
              }
            }
          }
        }";

        var response = await _httpClient.PostAsJsonAsync(
            "",
            new { query, variables = new { id = discussionId } }
        );

        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        var comments = data.GetProperty("data")
            .GetProperty("node")
            .GetProperty("comments")
            .GetProperty("nodes");

        return comments
            .EnumerateArray()
            .Select(c => new GitHubComment(
                c.GetProperty("id").GetString() ?? string.Empty,
                c.GetProperty("body").GetString() ?? string.Empty,
                c.GetProperty("author").GetProperty("login").GetString() ?? "ghost",
                c.GetProperty("createdAt").GetDateTime()
            ));
    }

    /// <inheritdoc />
    public async Task<GitHubDiscussion> PostReplyAsync(string discussionId, string body)
    {
        var mutation =
            @"
        mutation($discussionId: ID!, $body: String!) {
          addDiscussionComment(input: {discussionId: $discussionId, body: $body}) {
            comment {
              discussion {
                id
                title
                body
                url
                createdAt
                comments {
                  totalCount
                }
                category {
                  name
                }
              }
            }
          }
        }";

        var response = await _httpClient.PostAsJsonAsync(
            "",
            new { query = mutation, variables = new { discussionId, body } }
        );

        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        var d = data.GetProperty("data")
            .GetProperty("addDiscussionComment")
            .GetProperty("comment")
            .GetProperty("discussion");

        return new GitHubDiscussion(
            d.GetProperty("id").GetString() ?? string.Empty,
            d.GetProperty("title").GetString() ?? string.Empty,
            d.GetProperty("body").GetString() ?? string.Empty,
            d.GetProperty("url").GetString() ?? string.Empty,
            d.GetProperty("category").GetProperty("name").GetString() ?? string.Empty,
            d.GetProperty("comments").GetProperty("totalCount").GetInt32(),
            d.GetProperty("createdAt").GetDateTime()
        );
    }
}
