using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Play.Catalog.Contracts;
using Play.Catalog.Service.Dto;
using Play.Catalog.Service.Entities;
using Play.Common;

namespace Play.Catalog.Service.Controllers
{
    [Route("items")]
    [ApiController]
    public class ItemsController : ControllerBase
    {
        private readonly IRepository<Item> _itemsRepository;
        private readonly IPublishEndpoint _publishEndpoint;

        public ItemsController(IRepository<Item> itemsRepository, IPublishEndpoint publishEndpoint)
        {
            _itemsRepository = itemsRepository;
            _publishEndpoint = publishEndpoint;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemDto>>> GetAllAsync()
        {
            var itemDtos = (await _itemsRepository.GetAllAsync())
                .Select(itm => itm.AsDto());

            return Ok(itemDtos);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ItemDto>> GetByIdAsync(Guid id)
        {
            var item = await _itemsRepository.GetAsync(id);

            if (item is null)
            {
                return NotFound();
            }

            return Ok(item.AsDto());
        }

        [HttpDelete]
        public async Task<ActionResult> DeleteItemAsync(Guid id)
        {
            var item = await _itemsRepository.GetAsync(id);

            if (item is null)
            {
                return NotFound();
            }

            await _itemsRepository.DeleteAsync(item.Id);

            // send message about deleting
            await _publishEndpoint.Publish(new CatalogItemDeleted(id));

            return NoContent();

        }

        [HttpPost]
        public async Task<ActionResult<ItemDto>> AddItemAsync(CreateItemDto createItemDto)
        {
            var item = new Item
            {
                Name = createItemDto.Name,
                Description = createItemDto.Description,
                Price = createItemDto.Price,
                CreatedDate = DateTimeOffset.UtcNow
            };

            await _itemsRepository.AddAsync(item);

            // send message about creation
            await _publishEndpoint.Publish(new CatalogItemCreated(item.Id, item.Name, item.Description));

            return CreatedAtAction(nameof(GetByIdAsync), new { id = item.Id }, item); // stupid bug with routing I can't use Async method in case CreatedInAction
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult> UpdateItemAsync(Guid id, UpdateItemDto updateItemDto)
        {
            var existingItem = await _itemsRepository.GetAsync(id);

            if (existingItem is null)
            {
                return NotFound($"Item wit Id = {id} not found");
            }

            existingItem.Name = updateItemDto.Name;
            existingItem.Description = updateItemDto.Description;
            existingItem.Price = updateItemDto.Price;

            await _itemsRepository.UpdateAsync(existingItem);

            // send message about updating
            await _publishEndpoint.Publish(new CatalogItemUpdated(existingItem.Id, existingItem.Name, existingItem.Description));

            return NoContent();
        }
    }
}