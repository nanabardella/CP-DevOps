namespace DimDim.Application;
public sealed record PageRequest
{
    public int Page { get; }
    public int PageSize { get; }
    public long Offset => ((long)Page - 1) * PageSize;
    public PageRequest(int page = 1, int pageSize = 20)
    {
        if (page < 1) throw new ArgumentException("page deve ser um inteiro maior ou igual a 1.");
        if (pageSize is < 1 or > 100) throw new ArgumentException("pageSize deve estar entre 1 e 100.");
        Page = page; PageSize = pageSize;
    }
}
public sealed record PagedResult<T>(int Page, int PageSize, int TotalItems, int TotalPages, IReadOnlyList<T> Items)
{
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
    public static PagedResult<T> Create(PageRequest request, int total, IReadOnlyList<T> items) => new(request.Page, request.PageSize, total, (int)Math.Ceiling((double)total / request.PageSize), items);
}
