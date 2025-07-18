using API.Dto;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace API.Controllers
{
    [Authorize]
    public class NotificationController(INotificationRepository notificationRepository,IUserRepository userRepository, IMapper mapper):BaseApiController
    {
        [HttpPost]
        public async Task<ActionResult> SendNotification(NotificationDto dto)
        {
            if (dto.RecipientUsername == null)
            {
                return BadRequest("Username not found");
            }

            var recipient = await userRepository.GetUserByUsernameAsync(dto.RecipientUsername);
            if (recipient == null) return NotFound();
            var notification = new Notification
            {
                Content = dto.Content,
                RecipientUsername = dto.RecipientUsername,
                AppUserId=recipient.Id,
                Recipient=recipient,
                DateSent=DateTime.Now
            };
            notificationRepository.AddNotification(notification);
            if (await notificationRepository.SaveAllAsync())
                return Ok(mapper.Map<NotificationDto>(notification));

            return BadRequest("Failed to save message");
        }

        [HttpDelete("delete-notification")]
        public async Task<ActionResult>DeleteNotification(int notificationId)
        {
            var username=User.GetUsername();
            var notification= await notificationRepository.GetNotificationById(notificationId);
            if (notification.RecipientUsername==null||notification.RecipientUsername.ToLower() != username.ToLower())
            {
                return BadRequest("Unauthorized");
            }
            notificationRepository.DeleteNotification(notification);

            if (!await notificationRepository.SaveAllAsync())
                return BadRequest("Problem deleting message");

            return Ok("Notification deleted succesfully");

        }
        [HttpGet]
        public async Task<ActionResult> GetMyNotifications()
        {
            var username = User.GetUsername();

            var notifications = await notificationRepository.GetNotificationThread(username);

            return Ok(notifications);
        }
    }
}
