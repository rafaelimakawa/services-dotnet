﻿using Looplex.DotNet.Core.Application.ExtensionMethods;
using Looplex.DotNet.Core.Common.Exceptions;
using Looplex.DotNet.Core.Common.Utils;
using Looplex.DotNet.Core.Domain;
using Looplex.DotNet.Middlewares.ScimV2.Application.Abstractions.Services;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities.Groups;
using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.ExtensionMethods;
using ScimPatch;

namespace Looplex.DotNet.Services.ScimV2.InMemory.Services
{
    public class GroupService() : IGroupService
    {
        internal static IList<Group> Groups = [];
        
        public Task GetAllAsync(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            context.Plugins.Execute<IHandleInput>(context, cancellationToken);
            var page = context.GetRequiredValue<int>("Pagination.Page");
            var perPage = context.GetRequiredValue<int>("Pagination.PerPage");

            context.Plugins.Execute<IValidateInput>(context, cancellationToken);

            context.Plugins.Execute<IDefineRoles>(context, cancellationToken);

            context.Plugins.Execute<IBind>(context, cancellationToken);

            context.Plugins.Execute<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                var records = Groups
                    .Skip(PaginationUtils.GetOffset(perPage, page))
                    .Take(perPage)
                    .ToList();

                var result = new PaginatedCollection
                {
                    Records = records.Select(r => (object)r).ToList(),
                    Page = page,
                    PerPage = perPage,
                    TotalCount = Groups.Count
                };
                context.State.Pagination.TotalCount = Groups.Count;
                
                context.Result = result.ToJson(Group.Converter.Settings);
            }

            context.Plugins.Execute<IAfterAction>(context, cancellationToken);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context, cancellationToken);

            return Task.CompletedTask;
        }

        public Task GetByIdAsync(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var id = Guid.Parse(context.GetRequiredValue<string>("Id"));
            context.Plugins.Execute<IHandleInput>(context, cancellationToken);

            var group = Groups.FirstOrDefault(g => g.Id == id.ToString());
            if (group == null)
            {
                throw new EntityNotFoundException(nameof(Group), id.ToString());
            }
            context.Plugins.Execute<IValidateInput>(context, cancellationToken);
            
            context.Roles.Add("Group", group);
            context.Plugins.Execute<IDefineRoles>(context, cancellationToken);

            context.Plugins.Execute<IBind>(context, cancellationToken);

            context.Plugins.Execute<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                context.Result = ((Group)context.Roles["Group"]).ToJson();
            }

            context.Plugins.Execute<IAfterAction>(context, cancellationToken);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context, cancellationToken);

            return Task.CompletedTask;
        }
        
        public Task CreateAsync(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var json = context.GetRequiredValue<string>("Resource");
            var group = Resource.FromJson<Group>(json, out var messages);
            context.Plugins.Execute<IHandleInput>(context, cancellationToken);

            if (messages.Count > 0)
            {
                throw new EntityInvalidException(messages.ToList());
            }
            context.Plugins.Execute<IValidateInput>(context, cancellationToken);

            context.Roles.Add("Group", group);
            context.Plugins.Execute<IDefineRoles>(context, cancellationToken);

            context.Plugins.Execute<IBind>(context, cancellationToken);

            context.Plugins.Execute<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                Groups.Add(context.Roles["Group"]);

                context.Result = context.Roles["Group"].Id;
            }

            context.Plugins.Execute<IAfterAction>(context, cancellationToken);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context, cancellationToken);

            return Task.CompletedTask;
        }
        
        public async Task PatchAsync(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var json = context.GetRequiredValue<string>("Operations");
            await GetByIdAsync(context, cancellationToken);
            var group = (Group)context.Roles["Group"];
            var operations = OperationTracker.FromJson(group, json);
            context.Plugins.Execute<IHandleInput>(context, cancellationToken);

            if (operations.Count == 0)
            {
                throw new InvalidOperationException("List of operations can't be empty.");
            }
            context.Plugins.Execute<IValidateInput>(context, cancellationToken);

            context.Roles["Operations"] = operations;
            context.Plugins.Execute<IDefineRoles>(context, cancellationToken);

            context.Plugins.Execute<IBind>(context, cancellationToken);

            context.Plugins.Execute<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                foreach (var operationNode in context.Roles["Operations"])
                {
                    if (!await operationNode.TryApplyAsync())
                    {
                        throw operationNode.OperationException;
                    }
                }
            }

            context.Plugins.Execute<IAfterAction>(context, cancellationToken);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context, cancellationToken);
        }

        public async Task DeleteAsync(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var id = Guid.Parse(context.GetRequiredValue<string>("Id"));
            context.Plugins.Execute<IHandleInput>(context, cancellationToken);

            await GetByIdAsync(context, cancellationToken);
            var group = (Group)context.Roles["Group"];
            if (group == null)
            {
                throw new EntityNotFoundException(nameof(Group), id.ToString());
            }
            context.Plugins.Execute<IValidateInput>(context, cancellationToken);
            
            context.Plugins.Execute<IDefineRoles>(context, cancellationToken);

            context.Plugins.Execute<IBind>(context, cancellationToken);

            context.Plugins.Execute<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                Groups.Remove(context.Roles["Group"]);
            }

            context.Plugins.Execute<IAfterAction>(context, cancellationToken);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context, cancellationToken);
        }
    }
}
