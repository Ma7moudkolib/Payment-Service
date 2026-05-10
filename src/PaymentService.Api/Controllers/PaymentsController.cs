using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Abstractions;
using PaymentService.Application.DTOs;

namespace PaymentService.Api.Controllers;

[ApiController]
[Route("payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("process")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentDto>> ProcessPayment(
        [FromBody] ProcessPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var payment = await _paymentService.ProcessPaymentAsync(request, cancellationToken);
        return Ok(payment);
    }

    [HttpGet("{id:guid}/status")]
    [ProducesResponseType(typeof(PaymentStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentStatusDto>> GetStatus(
        Guid id,
        CancellationToken cancellationToken)
    {
        var status = await _paymentService.GetPaymentStatusAsync(id, cancellationToken);
        return Ok(status);
    }
}
