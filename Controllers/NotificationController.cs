﻿using API.Dto;
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
                Id = dto.Id,
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
                return BadRequest("Problem deleting notification");

            return Ok(new {message= "Notification has been deleted succesfully" });
        }

        [HttpDelete("delete-all-notifications")]
        public async Task<IActionResult> DeleteAllNotifications()
        {
            var username = User.GetUsername();
            if (username == null)
                throw new Exception("Username cannot be found");

            var deleted = await notificationRepository.DeleteByUsernameAsync(username);

            return Ok(new
            {
                message = "All notifications have been deleted"
            });
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
