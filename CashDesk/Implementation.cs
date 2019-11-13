using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace CashDesk
{
    /// <inheritdoc />
    public class DataAccess : IDataAccess
    {
        private MemberContext _context;
        /// <inheritdoc />
        public async Task InitializeDatabaseAsync()
        {
            if (_context == null)
                _context = new MemberContext();
            else
                throw new InvalidOperationException();           
            return;
        }

        private void checkContextNull()
        {
            if (_context == null)
            {
                throw new InvalidOperationException();
            }
                
        }

        /// <inheritdoc />
        public async Task<int> AddMemberAsync(string firstName, string lastName, DateTime birthday)
        {
            checkContextNull();

            //TODO invalid argument exception

            var member = new Member
            {
                FirstName = firstName,
                LastName = lastName,
                Birthday = birthday,
                Memberships = new List<Membership>(),
                Deposits = new List<Deposit>()
            };

            var duplicate = _context.Members.FirstOrDefault(p => p.LastName.Equals(lastName));
            if(duplicate != null)
            {
                throw new DuplicateNameException();
            }

            await _context.AddAsync(member);
            await _context.SaveChangesAsync();
            return member.MemberNumber;

        }

        /// <inheritdoc />
        public async Task DeleteMemberAsync(int memberNumber)
        {
            checkContextNull();

            var member =  _context.Members.Find(memberNumber);
            _context.Memberships.RemoveRange(member.Memberships);
            _context.Deposits.RemoveRange(member.Deposits);

            _context.Members.Remove(member);
            

            await _context.SaveChangesAsync();
            
            return;
        }

        /// <inheritdoc />
        public async Task<IMembership> JoinMemberAsync(int memberNumber)
        {
            checkContextNull();

            var member =  _context.Members.Find(memberNumber);
            
            var existingMembership = member.Memberships.FirstOrDefault(p => p.Active);

            if (existingMembership != null)
                throw new AlreadyMemberException();

            var newMembership = new Membership
            {
                Begin = DateTime.Now,
                Member = member,
                Active = true
            };

            if(member.Memberships == null)
            {
                member.Memberships = new List<Membership>();
            }
            
            member.Memberships.Add(newMembership);
            

            await _context.SaveChangesAsync();
            return newMembership;
        }

        /// <inheritdoc />
        public async Task<IMembership> CancelMembershipAsync(int memberNumber)
        {
            checkContextNull();

            var member = await _context.Members.FindAsync(memberNumber);

            var membership = member.Memberships.FirstOrDefault(p => p.Active);
            if (membership == null)
                throw new NoMemberException();

            membership.End = DateTime.Now;
            membership.Active = false;

            return membership;
        }

        /// <inheritdoc />
        public async Task DepositAsync(int memberNumber, decimal amount)
        {
            checkContextNull();

            var member = _context.Members.Find(memberNumber);

            if(member.Memberships.FirstOrDefault(p => p.Active) == null)
            {
                throw new NoMemberException();
            }

            var deposit = new Deposit
            {
                Membership = member.Memberships.FirstOrDefault(p => p.Active),
                Amount = amount
            };

            member.Deposits.Add(deposit);

            await _context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<IDepositStatistics>> GetDepositStatisticsAsync()
        {
            checkContextNull();

            var statistics = new List<DepositStatistics>();

            _context.Members.Load();
            foreach(var member in _context.Members)
            {
                if (member == null || member.Memberships == null)
                    continue;

                statistics.Add(new DepositStatistics{
                    Year = DateTime.Now.Year,
                    TotalAmount = member.Deposits.Sum(p => p.Amount),
                    Member = member
                });
            }

            return statistics;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if(_context != null)
            {
                _context.Dispose();
            }
        }
    }


    public class Member : IMember
    {

        [Key]
        public int MemberNumber { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public DateTime Birthday { get; set; }

        public ICollection<Membership> Memberships { get; set; }
        public ICollection<Deposit> Deposits { get; set; }
    }

    public class Membership : IMembership
    {
        public int MembershipId { get; set; }
        public Member Member { get; set; }

        public DateTime Begin { get; set; }

        public DateTime End { get; set; }

        public bool Active { get; set; }

        [NotMapped]
        IMember IMembership.Member => Member;
    }

    public class Deposit : IDeposit
    {
        public int DepositId { get; set; }
        public Membership Membership { get; set; }

        public decimal Amount { get; set; }

        [NotMapped]
        IMembership IDeposit.Membership => Membership;
    }

    public class DepositStatistics : IDepositStatistics
    {
        public int DepositStatisticsId { get; set; }
        public Member Member { get; set; }

        public int Year { get; set; }

        public decimal TotalAmount { get; set; }

        IMember IDepositStatistics.Member => Member;
    }
}
