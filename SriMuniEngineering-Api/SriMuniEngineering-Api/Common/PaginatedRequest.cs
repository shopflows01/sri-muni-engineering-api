namespace SriMuniEngineering_Api.Common;

public class PaginatedRequest
{
    private int _page = 1;
    private int _pageSize = 20;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 20 : value > 100 ? 100 : value;
    }

    public string? SortBy { get; set; }
    public string SortDirection { get; set; } = "desc"; // asc or desc
    public string? Search { get; set; }
}
