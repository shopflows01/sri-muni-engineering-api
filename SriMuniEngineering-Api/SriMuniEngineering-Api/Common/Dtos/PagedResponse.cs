namespace SriMuniEngineering_Api.Common.Dtos;

public class PagedResponse<T>
{
    public IEnumerable<T> Data { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public PagedResponse() { }

    public PagedResponse(IEnumerable<T> data, int count, int pageNumber, int pageSize)
    {
        Data = data;
        TotalCount = count;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}
