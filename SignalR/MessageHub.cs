using API.Data;
using API.Dto;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
    
    public class MessageHub(IMessageRepository messageRepository, IUserRepository userRepository, 
        IMapper mapper, IHubContext<PresenceHub> pressenceHub):Hub
    {

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var otherUser=httpContext?.Request.Query["user"];
            if(Context.User==null|| string.IsNullOrEmpty(otherUser))
            {
                throw new HubException("Cannot join group");
            }
            var groupName = GetGroupName(Context.User.GetUsername(), otherUser);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            var group=await AddToGroup(groupName);

            await Clients.Group(groupName).SendAsync("UpdatedGroup", group);

            var messages = await messageRepository.GetMessageThread(Context.User.GetUsername(), otherUser!);
            await Clients.Caller.SendAsync("ReceiveMessageThread",messages);
        }
        public async Task SendMessage(CreateMessageDto createMessageDto)
        {
            var username = Context.User?.GetUsername()?? throw new Exception("Cannot get user");
            if (username == createMessageDto.RecipientUsername.ToLower())
                throw new HubException("You cannot message yourself");

            var sender = await userRepository.GetUserByUsernameAsync(username);
            var recipient = await userRepository.GetUserByUsernameAsync(createMessageDto.RecipientUsername);

            if (recipient == null || sender == null)
                throw new HubException("You cannot send message at this time");
            var message = new Message
            {
                Sender = sender,
                Recipient = recipient,
                SenderUsername = sender.UserName,
                RecipientUsername = recipient.UserName,
                Content = createMessageDto.Content
            };
            var groupName = GetGroupName(sender.UserName, recipient.UserName);
            var group = await messageRepository.GetMessageGroup(groupName);

            if(group!=null&& group.Connections.Any(x=> x.Username == recipient.UserName))
            {
                message.DateRead=DateTime.UtcNow;
            }
            else
            {
                var connection=await PresenceTracker.GetConnectionsForUser(recipient.UserName);
                if (connection != null&& connection?.Count !=null)
                {
                    await pressenceHub.Clients.Clients(connection).SendAsync("NewMessageReceived",
                        new { username = sender.UserName, knownAs = sender.KnownAs });
                }
            }
                messageRepository.AddMessage(message);

            if (await messageRepository.SaveAllAsync())
            {
                await Clients.Group(groupName).SendAsync("NewMessage", mapper.Map<MessageDto>(message));
                return;
            }

            throw new HubException("Failed to save message");
        }
        public async Task UpdateMessage(UpdateMessageDto dto)
        {
            var message = await messageRepository.GetMessage(dto.MessageId);
            var username = Context.User?.GetUsername() ?? throw new HubException("Unauthorized");
            if (message == null)
                throw new HubException("Message not found");
            if (message.SenderUsername != username)
                throw new HubException("You can only edit your own messages");

            message.Content = dto.NewContent;
            message.Edited = true;
            message.UpdatedDate = DateTime.UtcNow;

            if (!await messageRepository.SaveAllAsync())
                throw new HubException("Failed to update message");
            var groupName = GetGroupName(message.SenderUsername, message.RecipientUsername);

            await Clients.Group(groupName).SendAsync("MessageUpdated", new
            {
                message.Id,
                message.Content,
                message.MessageSent,
                message.Edited,
                message.UpdatedDate
            });
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var group=await RemoveFromMessageGroup();
            await Clients.Group(group.Name).SendAsync("updatedGroup", group);
            await base.OnDisconnectedAsync(exception);
        }

        private async Task<Group> AddToGroup(string groupName)
        {
            var username= Context.User?.GetUsername()?? throw new Exception("Cannot get username");
            var group=await messageRepository.GetMessageGroup(groupName);
            var connection= new Connection { ConnectionId=Context.ConnectionId, Username = username };
            if (group == null)
            {
                group=new Group {Name= groupName };
                messageRepository.AddGroup(group);
            }
            group.Connections.Add(connection);
            if(await messageRepository.SaveAllAsync())return group;
            throw new HubException("Failed to join group");
        }
        public async Task DeleteMessage(int messageId)
        {
            var username = Context.User?.GetUsername() ?? throw new HubException("Cannot identify user");

            var message = await messageRepository.GetMessage(messageId);

            if (message == null || (message.SenderUsername != username && message.RecipientUsername != username))
                throw new HubException("Unauthorized");

            
            if (message.SenderUsername == username)
            {
                message.SenderDeleted = true;
                message.RecipientDeleted = true;
            }

            if (message.SenderDeleted && message.RecipientDeleted)
                Console.WriteLine(">>> BOTH deleted — performing hard delete!");
                messageRepository.DeleteMessage(message);

            if (!await messageRepository.SaveAllAsync())
                throw new HubException("Problem deleting message");

            var senderUsername = message.SenderUsername;
            var recipientUsername = message.RecipientUsername;

            var groupName = GetGroupName(senderUsername, recipientUsername);

            await Clients.Group(groupName).SendAsync("MessageDeleted", messageId);

            var updatedThreadForSender = await messageRepository.GetMessageThread(senderUsername, recipientUsername);
            var updatedThreadForRecipient = await messageRepository.GetMessageThread(recipientUsername, senderUsername);

            await Clients.Group(groupName).SendAsync("ReceiveMessageThread", updatedThreadForSender);
            await Clients.User(recipientUsername).SendAsync("ReceiveMessageThread", updatedThreadForRecipient);
        }



        private async Task<Group> RemoveFromMessageGroup()
        {
            var group=await messageRepository.GetGroupForConnection(Context.ConnectionId);
            var connection = group?.Connections.FirstOrDefault(x=>x.ConnectionId==Context.ConnectionId);
            if(connection != null)
            {
                messageRepository.RemoveConnection(connection);
                if(await messageRepository.SaveAllAsync()) return group;
            }
            throw new Exception("Failed to remove from group");

        }
        private string GetGroupName(string caller, string? other)
        {
            var stringCompare=string.CompareOrdinal(caller, other) < 0;
            return stringCompare? $"{caller}-{other}" : $"{other}-{caller}";
        }
    }
}
