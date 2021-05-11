using System;
using MessageBroker.Common.Pooling;
using MessageBroker.TCP.Binary;

namespace MessageBroker.Models.Async
{
    /// <summary>
    /// Ticket that can be subscribed to for checking if payload was successfully sent or not by <see cref="IClient"/>
    /// </summary>
    public class AsyncPayloadTicket: IPooledObject
    {
        
        /// <summary>
        /// Payload identifier used for notifying listeners in <see cref="OnStatusChanged"/>
        /// </summary>
        public Guid PayloadId { get; private set; }
        
        /// <summary>
        /// Identifier the object tracked by <see cref="ObjectPool"/>
        /// </summary>
        public Guid PoolId { get; set; }

        /// <summary>
        /// Event listeners can subscribe to be notified if status of payload changes
        /// </summary>
        public event Action<Guid, bool> OnStatusChanged;

        private IAsyncPayloadTicketHandler _handler;
        private bool _handlerSet;

        public IAsyncPayloadTicketHandler Handler
        {
            get
            {
                return _handler;
            }
            set
            {
                _handlerSet = true;
                _handler = value;
            }
        }

        public bool IsEmpty => OnStatusChanged == null;
   
        /// <summary>
        /// Checks if the status is set, if so an exception is thrown
        /// this is required so that more than once the status won't be updates
        /// otherwise we might run into weired issues
        /// </summary>
        private bool _isStatusSet;


        /// <summary>
        /// Will set the <see cref="PayloadId"/> and clear the status listeners
        /// </summary>
        /// <param name="payloadId"></param>
        public void Setup(Guid payloadId)
        {
            PayloadId = payloadId;
            ClearStatusListener();
        }
        
        /// <summary>
        /// Will clear any targets pointing to OnStatusChange
        /// calling this method is required for reusing SerializedPayload
        /// </summary>
        private void ClearStatusListener()
        {
            _isStatusSet = false;
            OnStatusChanged = null;
        }
        
        /// <summary>
        /// Will dispatch result to OnStatusChange event 
        /// </summary>
        /// <param name="success">True if the message was successfully sent, false otherwise</param>
        /// <exception cref="Exception">The status has been set once and cannot be called twice</exception>
        public void SetStatus(bool success)
        {
            lock (this)
            {
                if (_isStatusSet)
                {
                    throw new Exception($"{nameof(SerializedPayload)} status is already set");
                }
                
                _isStatusSet = true;
                
                // OnStatusChanged?.Invoke(PayloadId, success);
                Handler.OnStatusChanged(PayloadId, success);
            }
        }
        
    }

    public interface IAsyncPayloadTicketHandler
    {
        void OnStatusChanged(Guid payloadId, bool ack);
    }

}