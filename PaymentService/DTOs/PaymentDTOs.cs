namespace PaymentService.DTOs
{
    //when user wants to add money
    public class TopUpRequest
    {
        public Guid WalletId { get; set; }
        public decimal Amount { get; set; }
        public string? Note { get; set; }
    }
    //after top-up request
    public class PaymentResponse
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? GatewayRef { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    //WRAPPER 
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static ApiResponse<T> Successfull(string message, T data) =>new ApiResponse<T> { Success = true, Message = message, Data = data };

        public static ApiResponse<T> Fail(string message) => new ApiResponse<T> { Success = false, Message = message, Data = default };
    }
}
