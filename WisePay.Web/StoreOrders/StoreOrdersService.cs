using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WisePay.DataAccess;
using WisePay.Entities;
using WisePay.Web.Core.ClientInteraction;
using WisePay.Web.ExternalServices.Crawler;
using WisePay.Web.ExternalServices.Crawler.Responses;
using WisePay.Web.Internals;
using WisePay.Web.Purchases.Models;

namespace WisePay.Web.Purchases
{
    public class StoreOrdersService
    {
        private readonly WiseContext _db;
        private readonly CrawlerApi _crawlerApi;

        public StoreOrdersService(WiseContext db, CrawlerApi crawlerApi)
        {
            _db = db;
            _crawlerApi = crawlerApi;
        }

        public async Task<Purchase> CreatePurchase(CreateStoreOrdersModel model, int currentUserId)
        {
            var storeOrder = new StoreOrder
            {
                StoreId = model.StoreId,
                IsSubmitted = false
            };

            var purchase = new Purchase
            {
                Type = PurchaseType.Store,
                CreatorId = currentUserId,
                IsPayedOff = false,
                Name = model.Name,
                CreatedAt = DateTime.Now,
                StoreOrder = storeOrder
            };

            _db.Purchases.Add(purchase);
            await _db.SaveChangesAsync();

            var userPurchases = model.Users
                .Union(new List<int> { currentUserId })
                .Select(u => new UserPurchase
            {
                PurchaseId = purchase.Id,
                UserId = u,
                Status = PurchaseStatus.New
            });

            _db.UserPurchases.AddRange(userPurchases);
            await _db.SaveChangesAsync();

            return await _db.Purchases
                .Include(up => up.StoreOrder)
                .Include(up => up.UserPurchases)
                .ThenInclude(p => p.User)
                .Include(up => up.UserPurchases)
                .ThenInclude(p => p.Items)
                .Where(p => p.Id == purchase.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<UserPurchase> UpdateOrder
            (int purchaseId, SubmitOrderItemsModel model, int currentUserId)
        {
            var purchase = await _db.Purchases
                .Include(p => p.StoreOrder)
                .Include(p => p.UserPurchases)
                    .ThenInclude(up => up.User)
                .Where(up => up.Id == purchaseId)
                .FirstAsync();

            if (purchase.StoreOrder.IsSubmitted)
                throw new ApiException(401, "Purchase closed for change by creator", ErrorCode.ValidationError);

            var userPurchase = purchase.UserPurchases
                .Where(t => t.UserId == currentUserId)
                .FirstOrDefault();

            var prevItems = await _db.UserPurchaseItems
                .Include(t => t.UserPurchase)
                .Where(t => t.UserPurchase.PurchaseId == purchaseId)
                .Where(t => t.UserPurchase.UserId == currentUserId)
                .ToListAsync();

            var items = model.Items.Select(u => new UserPurchaseItem
            {
                UserPurchase = userPurchase,

                ItemId = u.ItemId,
                Number = u.Number,
                Price = u.Price
            });

            var prevUserPurchaseSum = userPurchase.Sum;
            userPurchase.Sum = items.Sum(item => item.Price);

            if (purchase.CreatorId != currentUserId)
            {
                purchase.TotalSum = (purchase.TotalSum ?? 0) - (prevUserPurchaseSum ?? 0) + userPurchase.Sum;
            }

            _db.UserPurchaseItems.RemoveRange(prevItems);
            _db.UserPurchaseItems.AddRange(items);
            await _db.SaveChangesAsync();

            return userPurchase;
        }

        public async Task SubmitOrder(int purchaseId, int currentUserId)
        {
            var purchase = await _db.Purchases
                .Include(p => p.StoreOrder)
                .Include(p => p.UserPurchases)
                    .ThenInclude(up => up.Items)
                .Where(up => up.Id == purchaseId)
                .FirstAsync();

            if (purchase.CreatorId != currentUserId)
                throw new ApiException(401, "Access denied", ErrorCode.AuthError);

            if (purchase.StoreOrder.IsSubmitted)
                throw new ApiException(400, "Order is already submitted", ErrorCode.InvalidAction);

            var emptyUserPurchases = purchase
                .UserPurchases
                .Where(up => up.Items.Count == 0);

            if (emptyUserPurchases.Count() == purchase.UserPurchases.Count)
                throw new ApiException(400, "Order is empty", ErrorCode.InvalidAction);

            _db.UserPurchases.RemoveRange(emptyUserPurchases);

            purchase.StoreOrder.IsSubmitted = true;
            await _db.SaveChangesAsync();
        }

        public async Task<ICollection<StoreResponse>> GetStores()
        {
            return await _crawlerApi.GetStores();
        }

        public async Task<StoreResponse> GetStoreContent(string storeId)
        {
            return await _crawlerApi.GetStoreContent(storeId);
        }

        public async Task<ICollection<ItemResponse>> GetItems(string categoryId, IEnumerable<string> itemIds)
        {
            return await _crawlerApi.GetItems(categoryId, itemIds);
        }
    }
}
