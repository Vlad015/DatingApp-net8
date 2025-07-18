using API.Dto;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
    public class NotificationHub(INotificationRepository notificationRepository, IUserRepository userRepository):Hub
    {
        public async Task SendNotification(NotificationDto dto)
        {
            var recipient = await userRepository.GetUserByUsernameAsync(dto.RecipientUsername);
            if (recipient == null)
            {
                throw new HubException("You cannot send message at this time");
            }
            var notification = new Notification
            {
                Content = dto.Content,
                Recipient = recipient,

            };
        }
    }
}
