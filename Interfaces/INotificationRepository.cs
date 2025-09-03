using API.Dto;
using API.Entities;

namespace API.Interfaces
{
    public interface INotificationRepository
    {
        void AddNotification(Notification notification);
        void DeleteNotification(Notification notification);
        Task<IEnumerable<NotificationDto>> GetNotificationThread(string username);
        Task<Notification?> GetNotificationById(int id);
        Task<List<Notification?>> DeleteByUsernameAsync(string username);
        Task<bool> SaveAllAsync();


    }
}
