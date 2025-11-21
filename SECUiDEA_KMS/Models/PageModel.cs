using CoreDAL.ORM;
using CoreDAL.ORM.Extensions;

namespace SECUiDEA_KMS.Models;

public class PageModel : SQLParam
{
    [DbParameter]
    public int PageNumber { get; set; } = 1;
    [DbParameter]
    public int PageSize { get; set; } = 10;
}
