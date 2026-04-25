using Microsoft.AspNetCore.Mvc;

namespace WebShop.Controllers;

[ApiController]
[Route("api/tranzila")]
public class TranzilaController : Controller
{
    private static readonly string tranzilaEndpoint = "https://secure.tranzila.com/cgi-bin/tranzila71u.cgi";
    private static readonly string merchantId = "YOUR_MERCHANT_ID";

    // shared memory storage
    private static readonly Dictionary<string, (decimal Amount, bool Paid)> orders = new();

    [HttpGet("buy")]
    public IActionResult Buy(decimal amount)
    {
        if (amount <= 0)
            return BadRequest("Invalid amount");

        var orderId = Guid.NewGuid().ToString();

        orders[orderId] = (amount, false);

        var paymentUrl =
            $"{tranzilaEndpoint}" +
            $"?sum={amount:0.00}" +
            $"&supplier={merchantId}" +
            $"&orderid={orderId}" +
            $"&success_url=https://localhost:5001/api/payment/success" +
            $"&fail_url=https://localhost:5001/api/payment/fail";

        return Redirect(paymentUrl);
    }

    [HttpPost("callback")]
    public async Task<IActionResult> Callback()
    {
        var form = await Request.ReadFormAsync();

        var status = form["Response"].ToString();
        var orderId = form["orderid"].ToString();

        if (!string.IsNullOrEmpty(orderId) && orders.ContainsKey(orderId))
        {
            if (status == "000")
            {
                var o = orders[orderId];
                orders[orderId] = (o.Amount, true);
            }
        }

        return Ok();
    }

    [HttpGet("success")]
    public IActionResult Success(string orderid)
    {
        if (orders.ContainsKey(orderid))
        {
            var o = orders[orderid];
            return Content($"<h2>Success</h2><p>{orderid}</p><p>Paid: {o.Paid}</p>", "text/html");
        }

        return Content("<h2>Success</h2>", "text/html");
    }

    [HttpGet("fail")]
    public IActionResult Fail()
    {
        return Content("<h2>Payment Failed</h2>", "text/html");
    }
}