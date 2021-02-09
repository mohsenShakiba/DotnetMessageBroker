﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MessageBroker.SocketServer.Abstractions;

namespace MessageBroker.SocketServer
{
    /// <summary>
    ///     SessionResolver is used to retrieve sessions based on session guid
    ///     this class is used by dispatcher
    /// </summary>
    // public class SessionResolver : ISessionResolver
    // {
    //     private readonly ConcurrentDictionary<Guid, IClientSession> _sesions;
    //
    //
    //     public SessionResolver()
    //     {
    //         _sesions = new ConcurrentDictionary<Guid, IClientSession>();
    //     }
    //
    //     public IReadOnlyList<IClientSession> Sessions => _sesions.Values.ToList();
    //
    //     public void Add(IClientSession session)
    //     {
    //         _sesions[session.SessionId] = session;
    //     }
    //
    //     public void Remove(Guid sessionId)
    //     {
    //         _sesions.TryRemove(sessionId, out _);
    //     }
    //
    //     public IClientSession Resolve(Guid guid)
    //     {
    //         _sesions.TryGetValue(guid, out var session);
    //         return session;
    //     }
    // }
}