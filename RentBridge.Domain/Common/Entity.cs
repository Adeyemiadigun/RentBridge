using System;
using System.Collections.Generic;
using System.Text;

namespace RentBridge.Domain.Common
{

    public abstract class Entity<TId> where TId : notnull
    {
        public TId Id { get; protected set; }
        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;
        protected void Raise(IDomainEvent e) => _domainEvents.Add(e);
        internal void ClearDomainEvents() => _domainEvents.Clear();
    }
}
