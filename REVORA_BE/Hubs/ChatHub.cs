using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using REVORA_BE.Services.Interfaces;

namespace REVORA_BE.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IServiceScopeFactory _scopeFactory;
        // Lưu trữ kết nối: UserId -> HashSet<ConnectionId>
        public static readonly ConcurrentDictionary<long, HashSet<string>> UserConnections = new();

        public ChatHub(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public override Task OnConnectedAsync()
        {
            var userIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdStr, out long userId))
            {
                UserConnections.AddOrUpdate(
                    userId,
                    new HashSet<string> { Context.ConnectionId },
                    (key, existing) =>
                    {
                        lock (existing)
                        {
                            existing.Add(Context.ConnectionId);
                        }
                        return existing;
                    }
                );
            }
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var userIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdStr, out long userId))
            {
                if (UserConnections.TryGetValue(userId, out var existing))
                {
                    lock (existing)
                    {
                        existing.Remove(Context.ConnectionId);
                    }
                    if (existing.Count == 0)
                    {
                        UserConnections.TryRemove(userId, out _);
                    }
                }
            }
            return base.OnDisconnectedAsync(exception);
        }
    }
}