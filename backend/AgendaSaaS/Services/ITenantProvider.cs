namespace AgendaSaaS.Services;

public interface ITenantProvider
{
    Guid TenantId { get; set; }
}