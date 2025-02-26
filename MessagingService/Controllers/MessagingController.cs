using Microsoft.AspNetCore.Mvc;
using MessagingService.DAL;
using MessagingService.Models;
using MessagingService.Services;

namespace MessagingService.Controllers
{
    [ApiController] [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly ILogger<MessagesController> _logger;
        private readonly MessageRepository _repository;
        private readonly MessageBroadcaster _broadcaster;

        public MessagesController(ILogger<MessagesController> logger, MessageRepository repository, MessageBroadcaster broadcaster)
        {
            _logger = logger;
            _repository = repository;
            _broadcaster = broadcaster;
        }

        // POST: api/messages
        // Принимает сообщение от клиента, задаёт серверную метку времени, сохраняет его в БД и транслирует через веб‑сокет
        [HttpPost]
        public async Task<IActionResult> PostMessage([FromBody] MessageRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Text) || request.Text.Length > 128)
            {
                _logger.LogWarning("Invalid message received: {Request}", request);
                return BadRequest("Invalid message. Text must be non-empty and not exceed 128 characters.");
            }

            Message message = new Message
            {
                Text = request.Text,
                Timestamp = DateTime.UtcNow, 
                OrderNumber = request.OrderNumber
            };

            _repository.InsertMessage(message);
            _logger.LogInformation("Message received and saved: OrderNumber={OrderNumber}, Text={Text}", message.OrderNumber, message.Text);
            
            await _broadcaster.BroadcastMessageAsync(message);
            return Ok(new { status = "Message received", message });
        }

        // GET: api/messages?start=2024-10-10T12:00:00&end=2025-10-10T12:10:00
        // Возвращает список сообщений за заданный диапазон дат
        [HttpGet]
        public IActionResult GetMessages(DateTime start, DateTime end)
        {
            if (start > end)
            {
                _logger.LogWarning("Invalid date range: start {start} more than end {end}", start, end);
                return BadRequest("The start date must be before the end date.");
            }

            var messages = _repository.GetMessages(start, end);

            if (messages.Count > 0)
            {
                _logger.LogInformation("Returned messages: {Count} from range {Start} to {End}", messages.Count,
                    start, end);
                return Ok(messages);
            }
            else
            {
                return NoContent();
            }
        }
    }
}