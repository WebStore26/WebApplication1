using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using WebApplication1.Models;
using WebApplication1.Requests;

namespace WebShop.Controllers;

[ApiController]
[Route("api/payment")]
public class PaymentController : Controller
{
    private static readonly string paypalEmail = "eddiestruz@gmail.com"; //VWB6M5TYBN9VS
    private static readonly string paypalMerchantId = "VWB6M5TYBN9VS";
    private readonly ILogger<PaymentController> _logger;

    private static readonly Dictionary<string, (decimal Amount, bool Paid)> orders = new();
    private const string Paypal_Url = "https://www.paypal.com/cgi-bin/webscr";
    private readonly string? _baseUrl;

    private readonly AppDb _db;

    public PaymentController(ILogger<PaymentController> logger, AppDb db, IConfiguration config)
    {
        _logger = logger;
        //baseUrl = "https://localhost:5001/api/payment";
        _baseUrl = config["App:BaseUrl"];
        _logger.LogInformation("Base URL: {BaseUrl}", _baseUrl);
        _db = db;
    }

    [HttpPost("create-order")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        _logger.LogInformation("CREATE ORDER called with ItemId: {ItemId}", request.ItemId);

        var item = await _db.Shop_Items
            .FirstOrDefaultAsync(i => i.Id == request.ItemId && i.IsActive);

        if (item == null)
        {
            _logger.LogWarning("Item not found: {ItemId}", request.ItemId);
            return BadRequest("Item not found");
        }

        var orderId = Guid.NewGuid().ToString();

        var order = new Order
        {
            OrderId = orderId,
            ItemId = item.Id,
            Amount = item.Price,
            IsPaid = false,
            Status = "Pending"
        };

        _db.Shop_Orders.Add(order);
        await _db.SaveChangesAsync();

        var returnUrl = $"{_baseUrl}/success?orderid={orderId}";
        var cancelUrl = $"{_baseUrl}/fail?orderid={orderId}";

        var redirectUrl =
            Paypal_Url +
            "?cmd=_xclick" +
            $"&business={paypalMerchantId}" +
            $"&item_name={Uri.EscapeDataString(item.Name)}" +
            $"&amount={item.Price:0.00}" +
            "&currency_code=ILS" +
            $"&return={Uri.EscapeDataString(returnUrl)}" +
            $"&cancel_return={Uri.EscapeDataString(cancelUrl)}" +
            $"&custom={orderId}" +
            "&landing_page=Billing";

        _logger.LogInformation("Order created {OrderId}", orderId);

        return Ok(new
        {
            orderId,
            redirectUrl
        });
    }

    // 🔴 STEP 2: PayPal IPN (REAL payment confirmation)
    [HttpPost("ipn")]
    public async Task<IActionResult> IPN()
    {
        _logger.LogInformation("🔥 IPN HIT");

        var form = await Request.ReadFormAsync();

        foreach (var key in form.Keys)
        {
            _logger.LogInformation("IPN: {Key} = {Value}", key, form[key]);
        }

        var paymentStatus = form["payment_status"].ToString();
        var orderId = form["custom"].ToString();
        var amountStr = form["mc_gross"].ToString();
        var txnId = form["txn_id"].ToString();

        if (string.IsNullOrEmpty(orderId))
            return BadRequest();

        var order = await _db.Shop_Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
        {
            _logger.LogWarning("Order not found: {OrderId}", orderId);
            return BadRequest();
        }

        if (!decimal.TryParse(amountStr, out var amount))
            return BadRequest();

        if (paymentStatus == "Completed" && order.Amount == amount)
        {
            order.IsPaid = true;
            order.Status = "Completed";
            order.PaidAt = DateTime.UtcNow;
            order.PaymentTransactionId = txnId;

            await _db.SaveChangesAsync();

            _logger.LogInformation("✅ Payment confirmed for Order {OrderId}", orderId);
        }
        else
        {
            _logger.LogWarning("⚠️ Payment invalid for Order {OrderId}", orderId);
        }

        return Ok();
    }

    [HttpGet("success")]
    public async Task<IActionResult> Success(string orderid)
    {
        var order = await _db.Shop_Orders.FirstOrDefaultAsync(o => o.OrderId == orderid);

        if (order == null)
            return Content("<h2>Order not found</h2>", "text/html");

        return Content($@"
            <h2>Payment Success</h2>
            <p>Order: {order.OrderId}</p>
            <p>Item: {order.ItemId}</p>
            <p>Paid: {order.IsPaid}</p>
            <p>Status: {order.Status}</p>
            <p>⚠️ If Paid=false, wait a few seconds and refresh</p>
        ", "text/html");
    }

    [HttpGet("fail")]
    public IActionResult Fail()
    {
        return Content("<h2>Payment Failed</h2>", "text/html");
    }
}