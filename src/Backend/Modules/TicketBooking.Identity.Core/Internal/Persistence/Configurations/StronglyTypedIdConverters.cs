using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TicketBooking.Identity.Domain;

namespace TicketBooking.Identity.Internal.Persistence.Configurations;

internal sealed class SystemUserIdConverter()
    : ValueConverter<SystemUserId, Guid>(id => id.Value, value => new SystemUserId(value));

internal sealed class SystemUserIdComparer()
    : ValueComparer<SystemUserId>(
        (left, right) => left == right,
        id => id.GetHashCode(),
        id => new SystemUserId(id.Value));

internal sealed class RoleIdConverter()
    : ValueConverter<RoleId, Guid>(id => id.Value, value => new RoleId(value));

internal sealed class RoleIdComparer()
    : ValueComparer<RoleId>(
        (left, right) => left == right,
        id => id.GetHashCode(),
        id => new RoleId(id.Value));

internal sealed class PermissionIdConverter()
    : ValueConverter<PermissionId, Guid>(id => id.Value, value => new PermissionId(value));

internal sealed class PermissionIdComparer()
    : ValueComparer<PermissionId>(
        (left, right) => left == right,
        id => id.GetHashCode(),
        id => new PermissionId(id.Value));
