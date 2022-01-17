using System;
using System.ComponentModel.DataAnnotations;

namespace SteamTrader.Domain
{
    public class Entity
    {
        [Key] 
        public Guid Id { get; protected set; } = new Guid();
        
        public DateTime Created { get; protected set; } = DateTime.UtcNow;
        
        public DateTime Updated { get; protected set; } = DateTime.UtcNow;
        
        public bool IsDeleted { get; protected set; }

        public void SetUpdated()
        {
            Updated = DateTime.UtcNow;
        }
    }
}