namespace SECUiDEA_KMS.Models;

/// <summary>
/// 클라이언트 리스트 페이징 응답
/// </summary>
public class ClientListDTO
{
    /// <summary>
    /// 클라이언트 목록
    /// </summary>
    public List<ClientServerEntity> Clients { get; set; } = new();

    /// <summary>
    /// 전체 개수
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 현재 페이지 번호
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// 페이지 크기
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 전체 페이지 수
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// 이전 페이지 존재 여부
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// 다음 페이지 존재 여부
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
}

