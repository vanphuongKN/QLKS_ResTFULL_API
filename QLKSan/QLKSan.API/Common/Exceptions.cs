namespace QLKSan.Common
{
    // Service ném các exception này; GlobalExceptionHandler chuyển thành HTTP status + ProblemDetails tương ứng

    // 404 Not Found: không tìm thấy tài nguyên
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }

    // 400 Bad Request: vi phạm quy tắc nghiệp vụ (phòng không trống, hoá đơn đã thanh toán...)
    public class BusinessException : Exception
    {
        public BusinessException(string message) : base(message) { }
    }

    // 409 Conflict: trùng dữ liệu (tên đăng nhập, CCCD, mã khuyến mãi...)
    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message) { }
    }
}
