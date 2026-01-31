using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Models.Webhooks;
using SD.ProjectName.Modules.Integrations.Application;
using SD.ProjectName.Modules.Integrations.Domain.Entities;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// API controller for managing webhook subscriptions
/// </summary>
[ApiController]
[Route("api/v1/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly WebhookSubscriptionService _subscriptionService;
    private readonly WebhookDeliveryService _deliveryService;

    public WebhooksController(
        WebhookSubscriptionService subscriptionService,
        WebhookDeliveryService deliveryService)
    {
        _subscriptionService = subscriptionService;
        _deliveryService = deliveryService;
    }

    /// <summary>
    /// Get the webhook event catalogue
    /// </summary>
    [HttpGet("events")]
    public ActionResult<WebhookEventCatalogueDto> GetEventCatalogue()
    {
        return Ok(new WebhookEventCatalogueDto
        {
            SupportedEvents = WebhookEventType.AllEventTypes
        });
    }

    /// <summary>
    /// Get all webhook subscriptions
    /// </summary>
    [HttpGet("subscriptions")]
    public async Task<ActionResult<List<WebhookSubscriptionDto>>> GetAllSubscriptions()
    {
        var subscriptions = await _subscriptionService.GetAllSubscriptionsAsync();
        
        var dtos = subscriptions.Select(s => new WebhookSubscriptionDto
        {
            Id = s.Id,
            Name = s.Name,
            EndpointUrl = s.EndpointUrl,
            SubscribedEvents = s.SubscribedEvents.Split(',', StringSplitOptions.RemoveEmptyEntries),
            Status = s.Status.ToString(),
            VerifiedAt = s.VerifiedAt,
            SecretRotatedAt = s.SecretRotatedAt,
            ConsecutiveFailures = s.ConsecutiveFailures,
            DegradedAt = s.DegradedAt,
            DegradedReason = s.DegradedReason,
            Description = s.Description,
            CreatedAt = s.CreatedAt,
            CreatedBy = s.CreatedBy,
            UpdatedAt = s.UpdatedAt,
            UpdatedBy = s.UpdatedBy
        }).ToList();
        
        return Ok(dtos);
    }

    /// <summary>
    /// Get a webhook subscription by ID
    /// </summary>
    [HttpGet("subscriptions/{id}")]
    public async Task<ActionResult<WebhookSubscriptionDto>> GetSubscription(int id)
    {
        var subscription = await _subscriptionService.GetSubscriptionByIdAsync(id);
        if (subscription == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = 404,
                Title = "Webhook subscription not found",
                Detail = $"Webhook subscription with ID {id} does not exist"
            });
        }
        
        var dto = new WebhookSubscriptionDto
        {
            Id = subscription.Id,
            Name = subscription.Name,
            EndpointUrl = subscription.EndpointUrl,
            SubscribedEvents = subscription.SubscribedEvents.Split(',', StringSplitOptions.RemoveEmptyEntries),
            Status = subscription.Status.ToString(),
            VerifiedAt = subscription.VerifiedAt,
            SecretRotatedAt = subscription.SecretRotatedAt,
            ConsecutiveFailures = subscription.ConsecutiveFailures,
            DegradedAt = subscription.DegradedAt,
            DegradedReason = subscription.DegradedReason,
            Description = subscription.Description,
            CreatedAt = subscription.CreatedAt,
            CreatedBy = subscription.CreatedBy,
            UpdatedAt = subscription.UpdatedAt,
            UpdatedBy = subscription.UpdatedBy
        };
        
        return Ok(dto);
    }

    /// <summary>
    /// Create a new webhook subscription
    /// </summary>
    [HttpPost("subscriptions")]
    public async Task<ActionResult<WebhookSubscriptionDto>> CreateSubscription(
        [FromBody] CreateWebhookSubscriptionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Invalid request",
                Detail = "Name is required"
            });
        }
        
        if (string.IsNullOrWhiteSpace(request.EndpointUrl))
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Invalid request",
                Detail = "EndpointUrl is required"
            });
        }
        
        if (request.SubscribedEvents == null || request.SubscribedEvents.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Invalid request",
                Detail = "At least one event type must be specified"
            });
        }
        
        // TODO: Get user ID from auth context
        var userId = "system";
        
        var subscription = await _subscriptionService.CreateSubscriptionAsync(
            request.Name,
            request.EndpointUrl,
            request.SubscribedEvents,
            userId,
            request.MaxRetryAttempts ?? 3,
            request.RetryDelaySeconds ?? 5,
            request.UseExponentialBackoff ?? true,
            request.Description);
        
        var dto = new WebhookSubscriptionDto
        {
            Id = subscription.Id,
            Name = subscription.Name,
            EndpointUrl = subscription.EndpointUrl,
            SubscribedEvents = subscription.SubscribedEvents.Split(',', StringSplitOptions.RemoveEmptyEntries),
            Status = subscription.Status.ToString(),
            VerifiedAt = subscription.VerifiedAt,
            SecretRotatedAt = subscription.SecretRotatedAt,
            ConsecutiveFailures = subscription.ConsecutiveFailures,
            DegradedAt = subscription.DegradedAt,
            DegradedReason = subscription.DegradedReason,
            Description = subscription.Description,
            CreatedAt = subscription.CreatedAt,
            CreatedBy = subscription.CreatedBy,
            UpdatedAt = subscription.UpdatedAt,
            UpdatedBy = subscription.UpdatedBy
        };
        
        return CreatedAtAction(nameof(GetSubscription), new { id = dto.Id }, dto);
    }

    /// <summary>
    /// Update a webhook subscription
    /// </summary>
    [HttpPut("subscriptions/{id}")]
    public async Task<ActionResult<WebhookSubscriptionDto>> UpdateSubscription(
        int id,
        [FromBody] UpdateWebhookSubscriptionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Invalid request",
                Detail = "Name is required"
            });
        }
        
        if (string.IsNullOrWhiteSpace(request.EndpointUrl))
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Invalid request",
                Detail = "EndpointUrl is required"
            });
        }
        
        if (request.SubscribedEvents == null || request.SubscribedEvents.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Invalid request",
                Detail = "At least one event type must be specified"
            });
        }
        
        // TODO: Get user ID from auth context
        var userId = "system";
        
        try
        {
            var subscription = await _subscriptionService.UpdateSubscriptionAsync(
                id,
                request.Name,
                request.EndpointUrl,
                request.SubscribedEvents,
                userId,
                request.MaxRetryAttempts ?? 3,
                request.RetryDelaySeconds ?? 5,
                request.UseExponentialBackoff ?? true,
                request.Description);
            
            var dto = new WebhookSubscriptionDto
            {
                Id = subscription.Id,
                Name = subscription.Name,
                EndpointUrl = subscription.EndpointUrl,
                SubscribedEvents = subscription.SubscribedEvents.Split(',', StringSplitOptions.RemoveEmptyEntries),
                Status = subscription.Status.ToString(),
                VerifiedAt = subscription.VerifiedAt,
                SecretRotatedAt = subscription.SecretRotatedAt,
                ConsecutiveFailures = subscription.ConsecutiveFailures,
                DegradedAt = subscription.DegradedAt,
                DegradedReason = subscription.DegradedReason,
                Description = subscription.Description,
                CreatedAt = subscription.CreatedAt,
                CreatedBy = subscription.CreatedBy,
                UpdatedAt = subscription.UpdatedAt,
                UpdatedBy = subscription.UpdatedBy
            };
            
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = 404,
                Title = "Webhook subscription not found",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Activate a webhook subscription
    /// </summary>
    [HttpPost("subscriptions/{id}/activate")]
    public async Task<ActionResult<WebhookSubscriptionDto>> ActivateSubscription(int id)
    {
        // TODO: Get user ID from auth context
        var userId = "system";
        
        try
        {
            var subscription = await _subscriptionService.ActivateSubscriptionAsync(id, userId);
            
            var dto = new WebhookSubscriptionDto
            {
                Id = subscription.Id,
                Name = subscription.Name,
                EndpointUrl = subscription.EndpointUrl,
                SubscribedEvents = subscription.SubscribedEvents.Split(',', StringSplitOptions.RemoveEmptyEntries),
                Status = subscription.Status.ToString(),
                VerifiedAt = subscription.VerifiedAt,
                SecretRotatedAt = subscription.SecretRotatedAt,
                ConsecutiveFailures = subscription.ConsecutiveFailures,
                DegradedAt = subscription.DegradedAt,
                DegradedReason = subscription.DegradedReason,
                Description = subscription.Description,
                CreatedAt = subscription.CreatedAt,
                CreatedBy = subscription.CreatedBy,
                UpdatedAt = subscription.UpdatedAt,
                UpdatedBy = subscription.UpdatedBy
            };
            
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = 404,
                Title = "Webhook subscription not found",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Pause a webhook subscription
    /// </summary>
    [HttpPost("subscriptions/{id}/pause")]
    public async Task<ActionResult<WebhookSubscriptionDto>> PauseSubscription(int id)
    {
        // TODO: Get user ID from auth context
        var userId = "system";
        
        try
        {
            var subscription = await _subscriptionService.PauseSubscriptionAsync(id, userId);
            
            var dto = new WebhookSubscriptionDto
            {
                Id = subscription.Id,
                Name = subscription.Name,
                EndpointUrl = subscription.EndpointUrl,
                SubscribedEvents = subscription.SubscribedEvents.Split(',', StringSplitOptions.RemoveEmptyEntries),
                Status = subscription.Status.ToString(),
                VerifiedAt = subscription.VerifiedAt,
                SecretRotatedAt = subscription.SecretRotatedAt,
                ConsecutiveFailures = subscription.ConsecutiveFailures,
                DegradedAt = subscription.DegradedAt,
                DegradedReason = subscription.DegradedReason,
                Description = subscription.Description,
                CreatedAt = subscription.CreatedAt,
                CreatedBy = subscription.CreatedBy,
                UpdatedAt = subscription.UpdatedAt,
                UpdatedBy = subscription.UpdatedBy
            };
            
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = 404,
                Title = "Webhook subscription not found",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Rotate the signing secret for a subscription
    /// </summary>
    [HttpPost("subscriptions/{id}/rotate-secret")]
    public async Task<ActionResult> RotateSigningSecret(int id)
    {
        // TODO: Get user ID from auth context
        var userId = "system";
        
        try
        {
            await _subscriptionService.RotateSigningSecretAsync(id, userId);
            return Ok(new { message = "Signing secret rotated successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = 404,
                Title = "Webhook subscription not found",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Delete a webhook subscription
    /// </summary>
    [HttpDelete("subscriptions/{id}")]
    public async Task<ActionResult> DeleteSubscription(int id)
    {
        await _subscriptionService.DeleteSubscriptionAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Get delivery history for a subscription
    /// </summary>
    [HttpGet("subscriptions/{id}/deliveries")]
    public async Task<ActionResult<List<WebhookDeliveryDto>>> GetDeliveryHistory(
        int id,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100)
    {
        var deliveries = await _deliveryService.GetDeliveryHistoryAsync(id, skip, take);
        
        var dtos = deliveries.Select(d => new WebhookDeliveryDto
        {
            Id = d.Id,
            WebhookSubscriptionId = d.WebhookSubscriptionId,
            EventType = d.EventType,
            CorrelationId = d.CorrelationId,
            Status = d.Status.ToString(),
            AttemptCount = d.AttemptCount,
            LastHttpStatusCode = d.LastHttpStatusCode,
            LastErrorMessage = d.LastErrorMessage,
            CreatedAt = d.CreatedAt,
            LastAttemptAt = d.LastAttemptAt,
            CompletedAt = d.CompletedAt,
            NextRetryAt = d.NextRetryAt
        }).ToList();
        
        return Ok(dtos);
    }

    /// <summary>
    /// Get failed deliveries for a subscription
    /// </summary>
    [HttpGet("subscriptions/{id}/failed-deliveries")]
    public async Task<ActionResult<List<WebhookDeliveryDto>>> GetFailedDeliveries(
        int id,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100)
    {
        var deliveries = await _deliveryService.GetFailedDeliveriesAsync(id, skip, take);
        
        var dtos = deliveries.Select(d => new WebhookDeliveryDto
        {
            Id = d.Id,
            WebhookSubscriptionId = d.WebhookSubscriptionId,
            EventType = d.EventType,
            CorrelationId = d.CorrelationId,
            Status = d.Status.ToString(),
            AttemptCount = d.AttemptCount,
            LastHttpStatusCode = d.LastHttpStatusCode,
            LastErrorMessage = d.LastErrorMessage,
            CreatedAt = d.CreatedAt,
            LastAttemptAt = d.LastAttemptAt,
            CompletedAt = d.CompletedAt,
            NextRetryAt = d.NextRetryAt
        }).ToList();
        
        return Ok(dtos);
    }
}
