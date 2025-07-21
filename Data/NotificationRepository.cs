using API.Dto;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class NotificationRepository(AppDbContext context, IMapper mapper) : INotificationRepository
    {
        public void AddNotification(Notification notification)
        {
            context.Notifications.Add(notification);
        }

        public void DeleteNotification(Notification notification)
        {
            context.Notifications.Remove(notification);
        }

        public async Task<IEnumerable<NotificationDto>> GetNotificationThread(string username)
        {
            var notifications = await context.Notifications
                .Include(n => n.Recipient)
                .Where(n => n.Recipient.UserName == username)
                .OrderByDescending(n => n.DateSent)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Content = n.Content,
                    DateSent = n.DateSent,
                    RecipientUsername=n.RecipientUsername
                })
                .ToListAsync();

            return notifications;
        }

        public async Task<bool> SaveAllAsync()
        {
            return await context.SaveChangesAsync() > 0;
        }

        public async Task<Notification?> GetNotificationById(int id)
        {
            return await context.Notifications.FindAsync(id);
        }
    }
}
