using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aegis.Core.Models;

namespace Aegis.Core.Interfaces;

/// <summary>
/// Repository interface for user management
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Create a new user
    /// </summary>
    /// <param name="user">User to create</param>
    /// <returns>Created user with generated ID</returns>
    Task<User> CreateAsync(User user);

    /// <summary>
    /// Get user by ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User or null if not found</returns>
    Task<User?> GetByIdAsync(Guid userId);

    /// <summary>
    /// Get user by username
    /// </summary>
    /// <param name="username">Username</param>
    /// <returns>User or null if not found</returns>
    Task<User?> GetByUsernameAsync(string username);

    /// <summary>
    /// Get user by email
    /// </summary>
    /// <param name="email">Email address</param>
    /// <returns>User or null if not found</returns>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Get user by phone number
    /// </summary>
    /// <param name="phoneNumber">Phone number</param>
    /// <returns>User or null if not found</returns>
    Task<User?> GetByPhoneNumberAsync(string phoneNumber);

    /// <summary>
    /// Update user information
    /// </summary>
    /// <param name="user">User with updated information</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateAsync(User user);

    /// <summary>
    /// Delete a user (soft delete - mark as deleted)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteAsync(Guid userId);

    /// <summary>
    /// Update user's online status
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="isOnline">Online status</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateOnlineStatusAsync(Guid userId, bool isOnline);

    /// <summary>
    /// Update user's last seen timestamp
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="lastSeen">Last seen timestamp</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateLastSeenAsync(Guid userId, DateTime lastSeen);

    /// <summary>
    /// Search users by username or display name
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="limit">Maximum results</param>
    /// <returns>List of matching users</returns>
    Task<List<User>> SearchAsync(string query, int limit = 20);

    /// <summary>
    /// Check if username is available
    /// </summary>
    /// <param name="username">Username to check</param>
    /// <returns>True if available</returns>
    Task<bool> IsUsernameAvailableAsync(string username);

    /// <summary>
    /// Get user's contacts
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of contacts</returns>
    Task<List<Contact>> GetContactsAsync(Guid userId);

    /// <summary>
    /// Add a contact
    /// </summary>
    /// <param name="contact">Contact to add</param>
    /// <returns>Added contact</returns>
    Task<Contact> AddContactAsync(Contact contact);

    /// <summary>
    /// Remove a contact
    /// </summary>
    /// <param name="contactId">Contact ID</param>
    /// <returns>True if removed successfully</returns>
    Task<bool> RemoveContactAsync(Guid contactId);

    /// <summary>
    /// Block a contact
    /// </summary>
    /// <param name="contactId">Contact ID</param>
    /// <returns>True if blocked successfully</returns>
    Task<bool> BlockContactAsync(Guid contactId);

    /// <summary>
    /// Unblock a contact
    /// </summary>
    /// <param name="contactId">Contact ID</param>
    /// <returns>True if unblocked successfully</returns>
    Task<bool> UnblockContactAsync(Guid contactId);
}
