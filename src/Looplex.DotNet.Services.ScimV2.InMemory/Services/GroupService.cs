﻿using Looplex.DotNet.Core.Application.ExtensionMethods;
using Looplex.DotNet.Core.Common.Exceptions;
using Looplex.DotNet.Core.Common.Utils;
using Looplex.DotNet.Core.Domain;
using Looplex.DotNet.Middlewares.ScimV2.Application.Abstractions.Services;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities;
using Looplex.DotNet.Middlewares.ScimV2.Domain.Entities.Groups;
using Looplex.OpenForExtension.Commands;
using Looplex.OpenForExtension.Context;
using Looplex.OpenForExtension.ExtensionMethods;

namespace Looplex.DotNet.Services.ScimV2.InMemory.Services
{
    public class GroupService() : IGroupService
    {
        private static readonly IList<Group> _groups = [];
        
        public Task GetAllAsync(IDefaultContext context, CancellationToken cancellationToken)
        {
            context.Plugins.Execute<IHandleInput>(context);
            var page = context.GetRequiredValue<int>("Pagination.Page");
            var perPage = context.GetRequiredValue<int>("Pagination.PerPage");

            context.Plugins.Execute<IValidateInput>(context);

            context.Plugins.Execute<IDefineActors>(context);

            context.Plugins.Execute<IBind>(context);

            context.Plugins.Execute<IBeforeAction>(context);

            if (!context.SkipDefaultAction)
            {
                var records = _groups
                    .Skip(PaginationUtils.GetOffset(perPage, page))
                    .Take(perPage)
                    .ToList();

                var result = new PaginatedCollection
                {
                    Records = records.Select(r => (object)r).ToList(),
                    Page = page,
                    PerPage = perPage,
                    TotalCount = _groups.Count
                };
                context.State.Pagination.TotalCount = _groups.Count;
                
                context.Result = result.ToJson(Group.Converter.Settings);
            }

            context.Plugins.Execute<IAfterAction>(context);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context);

            return Task.CompletedTask;
        }

        public Task GetByIdAsync(IDefaultContext context, CancellationToken cancellationToken)
        {
            var id = Guid.Parse(context.GetRequiredValue<string>("Id"));
            context.Plugins.Execute<IHandleInput>(context);

            var group = _groups.FirstOrDefault(g => g.Id == id.ToString());
            if (group == null)
            {
                throw new EntityNotFoundException(nameof(Group), id.ToString());
            }
            context.Plugins.Execute<IValidateInput>(context);
            
            context.Actors.Add("Group", group);
            context.Plugins.Execute<IDefineActors>(context);

            context.Plugins.Execute<IBind>(context);

            context.Plugins.Execute<IBeforeAction>(context);

            if (!context.SkipDefaultAction)
            {
                context.Result = ((Group)context.Actors["Group"]).ToJson();
            }

            context.Plugins.Execute<IAfterAction>(context);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context);

            return Task.CompletedTask;
        }
        
        public Task CreateAsync(IDefaultContext context, CancellationToken cancellationToken)
        {
            var json = context.GetRequiredValue<string>("Resource");
            var group = Resource.FromJson<Group>(json, out var messages);
            context.Plugins.Execute<IHandleInput>(context);

            if (messages.Count > 0)
            {
                throw new EntityInvalidException(messages.ToList());
            }
            context.Plugins.Execute<IValidateInput>(context);

            context.Actors.Add("Group", group);
            context.Plugins.Execute<IDefineActors>(context);

            context.Plugins.Execute<IBind>(context);

            context.Plugins.Execute<IBeforeAction>(context);

            if (!context.SkipDefaultAction)
            {
                var groupId = Guid.NewGuid();

                context.Actors["Group"].Id = groupId.ToString();
                _groups.Add(context.Actors["Group"]);

                context.Result = groupId;
            }

            context.Plugins.Execute<IAfterAction>(context);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(IDefaultContext context, CancellationToken cancellationToken)
        {
            var id = Guid.Parse(context.GetRequiredValue<string>("Id"));
            context.Plugins.Execute<IHandleInput>(context);

            var group = _groups.FirstOrDefault(g => g.Id == id.ToString());
            if (group == null)
            {
                throw new EntityNotFoundException(nameof(Group), id.ToString());
            }
            context.Plugins.Execute<IValidateInput>(context);

            context.Actors.Add("Group", group);
            context.Plugins.Execute<IDefineActors>(context);

            context.Plugins.Execute<IBind>(context);

            context.Plugins.Execute<IBeforeAction>(context);

            if (!context.SkipDefaultAction)
            {
                _groups.Remove(context.Actors["Group"]);
            }

            context.Plugins.Execute<IAfterAction>(context);

            context.Plugins.Execute<IReleaseUnmanagedResources>(context);

            return Task.CompletedTask;
        }
    }
}
