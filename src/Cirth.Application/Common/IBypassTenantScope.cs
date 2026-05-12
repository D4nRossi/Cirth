namespace Cirth.Application.Common;

/// <summary>
/// Commands implementing this interface skip TenantScopingBehavior.
/// Use only for bootstrap operations that run before tenant claims exist (e.g. user provisioning).
/// </summary>
public interface IBypassTenantScope { }
