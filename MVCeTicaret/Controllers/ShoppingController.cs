using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MVCeTicaret.Models;

namespace MVCeTicaret.Controllers
{
    public class ShoppingController : Controller
    {
        Context db;

        public ShoppingController()
        {
            db = new Context();
        }


        [HttpPost]
        public ActionResult AddToCart(int id, FormCollection frm)
        {
            if (Session["OnlineKullanici"] == null)
                return RedirectToAction("Login", "Login");

            int miktar = 1;
            if (frm["quantity"] != null)
                miktar = int.Parse(frm["quantity"]);

            ControlCart(id, miktar);

            return RedirectToAction("ProductDetail", "Product",new { id = id });
        }

        private void ControlCart(int id, int quantity = 1)
        {
            OrderDetail od = db.OrderDetails.FirstOrDefault(x => x.ProductID == id && x.CustomerID == TemporaryUserData.UserID && x.IsCompleted == false);

            if (od == null) // yeni kayıt
            {
                od = new OrderDetail();
                od.ProductID = id;
                od.CustomerID = TemporaryUserData.UserID;
                od.IsCompleted = false;
                od.UnitPrice = db.Products.Find(id).UnitPrice;
                od.Discount = db.Products.Find(id).Discount;
                od.OrderDate = DateTime.Now;

                if (db.Products.Find(id).UnitsInStock >= quantity)
                    od.Quantity = quantity;
                else
                    od.Quantity = db.Products.Find(id).UnitsInStock;

                od.TotalAmount = od.Quantity * od.UnitPrice * (1 - od.Discount);
                db.OrderDetails.Add(od);
            }
            else // update işlemi
            {
                if(db.Products.Find(id).UnitsInStock > od.Quantity + quantity)
                {
                    od.Quantity += quantity;
                    od.TotalAmount = od.Quantity * od.UnitPrice * (1 - od.Discount);
                }
            }
            db.SaveChanges();
        }

        public ActionResult AddToWishlist(int id)
        {
            if (Session["OnlineKullanici"] == null)
                return RedirectToAction("Login", "Login");

            ControlWishlist(id);

            return RedirectToAction("ProductDetail", "Product", new { id = id });
        }

        private void ControlWishlist(int id)
        {
            Wishlist wishlist = db.Wishlists.FirstOrDefault(x => x.ProductID == id && x.CustomerID == TemporaryUserData.UserID && x.IsActive == true);

            if (wishlist == null)
            {
                wishlist = new Wishlist();
                wishlist.ProductID = id;
                wishlist.CustomerID = TemporaryUserData.UserID;
                wishlist.IsActive = true;

                db.Wishlists.Add(wishlist);
                db.SaveChanges();
            }
        }

        public ActionResult Cart()
        {
            return View(db.OrderDetails.Where(x => x.IsCompleted == false && x.CustomerID == TemporaryUserData.UserID).ToList());
        }

        public ActionResult RemoveFromCart(int id)
        {
            db.OrderDetails.Remove(db.OrderDetails.Find(id));
            db.SaveChanges();

            return Redirect(Request.UrlReferrer.ToString());
        }

        [HttpPost]
        public ActionResult UpdateQuantity(int id, FormCollection frm)
        {
            OrderDetail od = db.OrderDetails.Find(id);
            od.Quantity = int.Parse(frm["quantity"]);
            od.TotalAmount = od.Quantity * od.UnitPrice * (1 - od.Discount);

            db.SaveChanges();
            return Redirect(Request.UrlReferrer.ToString());
        }

        public ActionResult AddToWishlistFromCart(int id)
        {
            int productID = db.OrderDetails.Find(id).ProductID;
            ControlWishlist(productID);

            db.OrderDetails.Remove(db.OrderDetails.Find(id));
            db.SaveChanges();

            return Redirect(Request.UrlReferrer.ToString());
        }

        public ActionResult Wishlist()
        {
            return View(db.Wishlists.Where(x => x.CustomerID == TemporaryUserData.UserID && x.IsActive == true).ToList());
        }

        public ActionResult RemoveFromWishlist(int id)
        {
            Wishlist wishlist = db.Wishlists.Find(id);
            wishlist.IsActive = false;
            db.SaveChanges();

            return RedirectToAction("Wishlist", "Shopping");
        }

        public ActionResult AddToCartFromWishlist(int id)
        {
            int productID = db.Wishlists.Find(id).ProductID;
            ControlCart(productID);

            Wishlist wishlist = db.Wishlists.Find(id);
            wishlist.IsActive = false;
            db.SaveChanges();

            return RedirectToAction("Wishlist", "Shopping");
        }

        public ActionResult GoToPayment()
        {
            List<OrderDetail> cart = db.OrderDetails.Where(x => x.IsCompleted == false && x.CustomerID == TemporaryUserData.UserID).ToList();

            foreach (var item in cart)
            {
                if (item.Product.UnitsInStock < item.Quantity)
                    return RedirectToAction("UpdateCart", "Shopping");
            }

            ViewBag.OrderDetails = cart;
            ViewBag.PaymentTypes = db.PaymentTypes.ToList();

            return View(db.Customers.Find(TemporaryUserData.UserID));
        }

        public ActionResult UpdateCart()
        {
            return View(db.OrderDetails.Where(x => x.IsCompleted == false && x.CustomerID == TemporaryUserData.UserID).ToList());
        }

        [HttpPost]
        public ActionResult CompleteShopping(FormCollection frm)
        {
            Payment payment = new Payment();
            payment.Type = int.Parse(frm["paymentType"]);
            payment.CreditAmount = 20000;
            payment.DebitAmount = 20000;
            payment.Balance = 20000;
            payment.PaymentDateTime = DateTime.Now;

            db.Payments.Add(payment);

            if(frm["update"] == "on")
            {
                Customer customer = db.Customers.Find(TemporaryUserData.UserID);
                customer.FirstName = frm["FirstName"];
                customer.LastName = frm["LastName"];
                customer.Address = frm["Address"];
                customer.City = frm["City"];
            }

            ShippingDetail sp = new ShippingDetail();
            sp.FirstName = frm["FirstName"];
            sp.LastName = frm["LastName"];
            sp.Address = frm["Address"];
            sp.City = frm["City"];

            db.ShippingDetails.Add(sp);
            db.SaveChanges();

            List<OrderDetail> cart = db.OrderDetails.Where(x => x.CustomerID == TemporaryUserData.UserID && x.IsCompleted == false).ToList();
            foreach (var item in cart)
            {
                item.IsCompleted = true;
                item.Product.UnitsInStock -= item.Quantity;

                Order order = new Order()
                {
                    PaymentID = payment.PaymentID,
                    ShippingID = sp.ShippingID,
                    OrderDetailID = item.OrderDetailID,
                    Discount = item.Discount,
                    TotalAmount = item.TotalAmount,
                    IsCompleted = true,
                    OrderDate = DateTime.Now,
                    Dispatched = false,
                    DispatchDate = DateTime.Now.AddDays(3),
                    Shipped = false,
                    ShippedDate = DateTime.Now.AddDays(4),
                    Deliver = false,
                    DeliveryDate = DateTime.Now.AddDays(5),
                    CancelOrder = false
                };

                db.Orders.Add(order);
            }

            db.SaveChanges();
            return RedirectToAction("FinishShopping", "Shopping");
        }

        public ActionResult FinishShopping()
        {
            return View();
        }
    }
}