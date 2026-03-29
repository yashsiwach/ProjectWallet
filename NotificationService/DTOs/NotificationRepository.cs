using MongoDB.Driver;
using NotificationService.Models;

namespace NotificationService.DTOs;

public class NotificationRepository
{
    private readonly IMongoCollection<Notification> _collection;
    private readonly ILogger<NotificationRepository> _logger;

    public NotificationRepository(
        IConfiguration config,
        ILogger<NotificationRepository> logger)
    {
        _logger = logger;

       
        var connectionString = config["MongoDB:ConnectionString"];
        var databaseName = config["MongoDB:DatabaseName"];
        var collectionName = config["MongoDB:CollectionName"];

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _collection = database.GetCollection<Notification>(collectionName);

        _logger.LogInformation("MongoDB connected to {Database}", databaseName);

        
        var indexKey = Builders<Notification>.IndexKeys.Ascending(n => n.UserId).Descending(n => n.CreatedAt);

        _collection.Indexes.CreateOne(new CreateIndexModel<Notification>(indexKey));
    }

    public async Task InsertAsync(Notification notification)
    {
        await _collection.InsertOneAsync(notification);
        _logger.LogInformation("Notification saved for UserId: {UserId}", notification.UserId);
    }

    public async Task<List<Notification>> GetByUserIdAsync(string userId, int page, int pageSize)
    {
        return await _collection
            .Find(n => n.UserId == userId)
            .SortByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
    }

    public async Task<long> CountByUserIdAsync(string userId)
    {
        return await _collection.CountDocumentsAsync(n => n.UserId == userId);
    }

    public async Task<long> CountUnreadAsync(string userId)
    {
        return await _collection.CountDocumentsAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task MarkAsReadAsync(string id, string userId)
    {
        var filter = Builders<Notification>.Filter.Where(n => n.Id == id && n.UserId == userId);

        var update = Builders<Notification>.Update.Set(n => n.IsRead, true);

        await _collection.UpdateOneAsync(filter, update);
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        var filter = Builders<Notification>.Filter.Where(n => n.UserId == userId && !n.IsRead);

        var update = Builders<Notification>.Update.Set(n => n.IsRead, true);

        await _collection.UpdateManyAsync(filter, update);
    }
}
