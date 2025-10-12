namespace HW.Application.DTOs.Queries;

public record GetUsersQuery(DateTime? FromDate, DateTime? ToDate, PaginationRequest Pagination);