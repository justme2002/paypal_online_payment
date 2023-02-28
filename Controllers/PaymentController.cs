using PayPal.Api;
using asp_net_core_paypal.config;
using Microsoft.AspNetCore.Mvc;
using System.Web;

namespace asp_net_core_paypal.Controllers;

public class PaymentController : ControllerBase
{
  private readonly IConfiguration configuration;
  private readonly IHttpContextAccessor httpContextAccessor;
  public Payment? Payment { get; set; }
  public PaymentController(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
  {
    this.configuration = configuration;
    this.httpContextAccessor = httpContextAccessor;
  }

  [HttpGet("/PaymentWithPaypal")]
  public IActionResult PaymentWithPaypal(string PayerId, string Cancel = null)
  {
    string ClientId = configuration.GetValue<string>("Paypal:ClientId");
    string ClientSecret = configuration.GetValue<string>("Paypal:ClientSecret");
    APIContext ApiContext = PaypalConfiguration.GetApiContext(
      ClientId, 
      ClientSecret
    );
    if (string.IsNullOrEmpty(PayerId))
    {
      string BaseURI = "https://localhost:7204/PaymentWithPaypal?";
      var Guid = Convert.ToString(new Random().Next(100000));
      var CreatedPayment = this.CreatePayment(ApiContext, BaseURI+"guid="+Guid);
      var Links = CreatedPayment.links.GetEnumerator();
      string PaypalRedirectUrl = String.Empty;
      while (Links.MoveNext())
      {
        Links lnk = Links.Current;
        if (lnk.rel.ToLower().Trim().Equals("approval_url"))
        {
          PaypalRedirectUrl = lnk.href;
        }
      }
      this.httpContextAccessor.HttpContext!.Session.SetString("payment", CreatedPayment.id);
      return Ok(PaypalRedirectUrl);
    }
    else 
    {
      var PaymentId = httpContextAccessor.HttpContext!.Session.GetString("payment");
      var executedPayment = this.ExecutePayment(ApiContext, PayerId, PaymentId! as string);
      if (executedPayment.state.ToLower() != "approved")
      {
        return BadRequest(new {Message = "Error"});
      }
      return Ok(executedPayment.transactions[0]);
    }
  }

  private Payment ExecutePayment(APIContext ApiContext, string PayerId, string PaymentId)
  {
    var PaymentExecution = new PaymentExecution()
    {
      payer_id = PayerId
    };
    this.Payment = new Payment
    {
      id = PaymentId
    };
    return this.Payment.Execute(ApiContext, PaymentExecution);
  }

  private Payment CreatePayment(APIContext ApiContext, string RedirectUrl)
  {
    var ItemList = new ItemList
    {
      items = new List<Item>()
    };

    ItemList.items.Add(new Item()
    {
      name = "Item Name comes here",
      currency = "USD",
      price = "1",
      quantity = "1",
      sku = "sku"
    });
    var Payer = new Payer()
    {
      payment_method = "paypal"
    };
    var RedirUrls = new RedirectUrls()
    {
      cancel_url = RedirectUrl + "&Cancel=true",
      return_url = RedirectUrl
    };
    var Amount = new Amount()
    {
      currency = "USD",
      total = "1"
    };
    var TransactionList = new List<Transaction>();
    TransactionList.Add(new Transaction()
    {
      description = "Transaction Description",
      invoice_number = Guid.NewGuid().ToString(),
      amount = Amount,
      item_list = ItemList
    }); 
    this.Payment = new Payment()
    {
      intent = "sale",
      payer = Payer,
      transactions = TransactionList,
      redirect_urls = RedirUrls
    };
    return this.Payment.Create(ApiContext);
  }
}