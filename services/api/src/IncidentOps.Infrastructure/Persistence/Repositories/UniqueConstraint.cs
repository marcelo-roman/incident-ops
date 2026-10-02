using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace IncidentOps.Infrastructure.Persistence.Repositories;

internal static class UniqueConstraint
{
    private const int DuplicateKeyRow = 2601;
    private const int UniqueConstraintViolation = 2627;

    public static bool IsViolatedBy(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: DuplicateKeyRow or UniqueConstraintViolation };
}
