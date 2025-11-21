namespace SECUiDEA_KMS.Models;

public class ResultEntity
{
    public bool IsSuccess => ErrorCode == Consts.DbErrorCode_Success;
    public string ErrorCode { get; set; } = Consts.DbErrorCode_Success;
    public string ErrorMessage { get; set; } = Consts.DbErrorMessage_Success;
}

public class TotalCountEntity
{
    public int TotalCount { get; set; }
}