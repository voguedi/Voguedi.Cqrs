using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Voguedi.Commands;
using Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.Commands;
using Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.Stores;
using Voguedi.Utils;

namespace Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotesController : ControllerBase
    {
        #region Private Fields

        readonly ICommandSender commandSender;
        readonly INoteStore store;

        #endregion

        #region Ctors

        public NotesController(ICommandSender commandSender, INoteStore store)
        {
            this.commandSender = commandSender;
            this.store = store;
        }

        #endregion

        #region Public Methods

        [HttpPost("create")]
        public async Task<IActionResult> Create(string title, string content)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentNullException(nameof(title));

            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentNullException(nameof(content));

            var result = await commandSender.SendAsync(new CreateNoteCommand(ObjectId.NewObjectId().ToString(), title, content));

            if (result.Succeeded)
                return NoContent();

            return BadRequest(result.Exception);
        }

        [HttpPost("modify")]
        public async Task<IActionResult> Modify(string id, string title, string content)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentNullException(nameof(id));

            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentNullException(nameof(title));

            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentNullException(nameof(content));

            var result = await commandSender.SendAsync(new ModifyNoteCommand(id, title, content));

            if (result.Succeeded)
                return NoContent();

            return BadRequest(result.Exception);
        }

        [HttpGet("id")]
        public async Task<IActionResult> Get(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentNullException(nameof(id));

            var result = await store.GetAsync(id);

            if (result.Succeeded)
                return Ok(result.Data);

            return BadRequest(result.Exception);
        }

        #endregion
    }
}