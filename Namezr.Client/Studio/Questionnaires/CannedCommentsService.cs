using System.Net.Http.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using System;
using System.Net;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;

namespace Namezr.Client.Studio.Questionnaires;

[RegisterScoped]
public class CannedCommentsService
{
    private readonly HttpClient _httpClient;

    public CannedCommentsService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<CannedCommentModel>> GetCannedCommentsAsync()
    {
        var result = await _httpClient.GetFromJsonAsync<List<CannedCommentModel>>("/api/canned-comments");
        return result ?? new List<CannedCommentModel>();
    }

    public async Task<bool> CreateCannedCommentAsync(CannedCommentSaveRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/canned-comments", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateCannedCommentAsync(CannedCommentSaveRequest request)
    {
        if (request.Id == null)
            throw new ArgumentException("Id is required for update.");

        var response = await _httpClient.PutAsJsonAsync($"/api/canned-comments/{request.Id}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteCannedCommentAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"/api/canned-comments/{id}");
        return response.IsSuccessStatusCode;
    }
}