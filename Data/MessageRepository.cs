﻿using API.Dto;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class MessageRepository(AppDbContext context, IMapper mapper) : IMessageRepository
    {
        public void AddGroup(Group group)
        {
            context.Groups.Add(group);
        }

        public void AddMessage(Message message)
        {
            context.Messages.Add(message);
        }

        public void DeleteMessage(Message message)
        {
            Console.WriteLine(">>> Deleting message from DB: " + message.Id);
            context.Messages.Remove(message);
        }

        public async Task<Connection?> GetConnection(string connectionId)
        {
            return await context.Connections.FindAsync(connectionId);
        }

        public async Task<Group?> GetGroupForConnection(string connectionId)
        {
            return await context.Groups
                .Include(x=>x.Connections)
                .Where(x=>x.Connections.Any(c=>c.ConnectionId == connectionId))
                .FirstOrDefaultAsync();
        }

        public async Task<Message?> GetMessage(int id)
        {
            return await context.Messages.FindAsync(id);
        }

        public async Task<Group?> GetMessageGroup(string groupName)
        {
            return await context.Groups.Include(x => x.Connections)
                .FirstOrDefaultAsync(x => x.Name == groupName);
        }

        public async Task<PagedList<MessageDto>> GetMessagesForUser(MessageParams messageParams)
        {
            var query = context.Messages
                .OrderByDescending(x => x.MessageSent)
                .AsQueryable();

            query = messageParams.Container
                switch
            {
                "Inbox" => query.Where(x => x.RecipientUsername == messageParams.Username
                && x.RecipientDeleted == false),
                "Outbox" => query.Where(x => x.SenderUsername == messageParams.Username
                && x.SenderDeleted == false),
                _ => query.Where(x => x.RecipientUsername == messageParams.Username && x.DateRead == null)
            };
            var messages = query.ProjectTo<MessageDto>(mapper.ConfigurationProvider);

            return await PagedList<MessageDto>.CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
        }

        public async Task<IEnumerable<MessageDto>> GetMessageThread(string currentUsername, string recipientUsername)
        {
            var messages = await context.Messages
                .Where(x =>
                    (x.RecipientUsername == currentUsername && x.RecipientDeleted == false && x.SenderUsername == recipientUsername)
                    ||
                    (x.SenderUsername == currentUsername && x.SenderDeleted == false && x.RecipientUsername == recipientUsername)
                )
                .OrderBy(x => x.MessageSent)
                .ProjectTo<MessageDto>(mapper.ConfigurationProvider)
                .ToListAsync();
            var unreadMessages = messages.Where(x => x.DateRead == null &&
                x.RecipientUsername == recipientUsername).ToList();
            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.DateRead = DateTime.UtcNow;
                }
                await context.SaveChangesAsync();
            }
            return messages;
        }

        public void RemoveConnection(Connection connection)
        {
             context.Connections.Remove(connection);
        }

        public async Task<bool> SaveAllAsync()
        {
            return await context.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<LatestMessageDto>> GetLatestMessageForUser(string username)
        {
            var messages = await context.Messages
                .Where(m => m.SenderUsername == username || m.RecipientUsername == username)
                .OrderByDescending(m => m.MessageSent)
                .Include(m => m.Sender).ThenInclude(p => p.Photos)
                .Include(m => m.Recipient).ThenInclude(p => p.Photos)
                .ToListAsync(); // ⚠️ mutăm în memorie

            var latestMessages = messages
                .Select(m => new
                {
                    Message = m,
                    OtherUsername = m.SenderUsername == username ? m.RecipientUsername : m.SenderUsername
                })
                .GroupBy(x => x.OtherUsername)
                .Select(g => g.First())
                .Select(x => new LatestMessageDto
                {
                    Username = x.OtherUsername,
                    PhotoUrl = x.Message.SenderUsername == username
                        ? x.Message.Recipient.Photos.FirstOrDefault(p => p.IsMain)?.Url
                        : x.Message.Sender.Photos.FirstOrDefault(p => p.IsMain)?.Url,
                    LastMessageContent = x.Message.Content,
                    LastMessageSent = x.Message.MessageSent
                });

            return latestMessages;
        }

    }
}
