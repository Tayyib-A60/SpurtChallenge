using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Core;
using API.DTOs;
using API.Extension;
using API.Models;
using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace API.Controllers
{
    [Route("api/spurt")]
    [ApiController]
    public class SpurtController : ControllerBase
    {
        private IMapper _mapper { get; }
        private ISpurtRepository _repository { get; }
        private IOptions<CloudinarySettings> _cloudinaryConfig { get; }
        private Cloudinary _cloudinary;
        private PhotoSettings _photoSettings { get; }
        private Microsoft.Extensions.Configuration.IConfiguration _configuration { get; }


        public SpurtController(ISpurtRepository repository, IMapper mapper, IOptions<CloudinarySettings> cloudinaryConfig, IOptionsSnapshot<PhotoSettings> options, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _repository = repository;
            _mapper = mapper;
            _cloudinaryConfig = cloudinaryConfig;
            _photoSettings = options.Value;
            _configuration = configuration;

            Account acc = new Account(
                _cloudinaryConfig.Value.CloudName,
                _cloudinaryConfig.Value.ApiKey,
                _cloudinaryConfig.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(acc);
        }
        
        [HttpPost("createEvent")]
        public async Task<IActionResult> CreateEvent([FromBody] EventDTO eventDTO)
        {
            var eventToCreate = _mapper.Map<Event>(eventDTO);
            if (eventToCreate == null)
                return BadRequest("Event cannot be null");
            if (await _repository.EntityExists(eventToCreate))
                return BadRequest("Event has already been created");
            eventToCreate.DateCreated = DateTime.Now;
            _repository.Add(eventToCreate);
            await _repository.SaveAllChanges();
            return Ok();
        }

        [HttpPost ("createCustomer")]
        public async Task<IActionResult> CreateClient ([FromBody] UserDTO userDTO)
        {
            var user = _mapper.Map<User> (userDTO);
            user.Role = Role.Admin;
            user.EmailVerified = false;
            if (await _repository.EntityExists(user))
                return BadRequest ("User already exists");
            try {
                var userCreated = await _repository.CreateUser (userDTO);
                if(userCreated.Email == user.Email) {
                    StringValues origin;
                    var token = _repository.CreateToken(user);
                    Request.Headers.TryGetValue("Origin", out origin);
                    Message message = new Message();
                    message.Subject = "Account Confirmation";
                    message.FromEmail = "noreply@234spaces.com";
                    message.FromName = "234Spaces Admin";
                    message.ToName = user.Name;
                    message.ToEmail = user.Email;
                    message.PlainContent = null;
                    message.HtmlContent = $"<div><span> Click the link to activate your account</span><a>{origin.ToString()}/confirm-email?token={token}</a></div>";
                    SendEmail(message);
                }
                return Ok ();
            } catch (Exception ex) {
                return BadRequest (ex.Message);
            }
        }
        
        [HttpPost ("authenticate")]
        public IActionResult Authenticate ([FromBody] UserDTO userDTO) {
            var user = _repository.Authenticate (userDTO.Email, userDTO.Password);
            if (user == null)
                return Unauthorized();
            if(!user.EmailVerified)
                return Unauthorized();
            var tokenString = _repository.CreateToken(user);
            return Ok (new {
                Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Token = tokenString,
                    Roles = user.Role.ToString()
            });
        }


        [HttpPost ("subscribe")]
        public async Task<IActionResult> Subscribe ([FromBody] SubscriberDTO subscriberDTO)
        {
            var subscriberToAdd = _mapper.Map<Subscriber>(subscriberDTO);
            if (subscriberToAdd == null)
                return BadRequest("Subscriber cannot be null");
            if (await _repository.EntityExists(subscriberToAdd))
                return BadRequest("Subscriber has already been added");
            subscriberToAdd.DateCreated = DateTime.Now;
            _repository.Add(subscriberToAdd);
            await _repository.SaveAllChanges();
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllEvents () {
            var events = await _repository.GetEvents ();
            var eventsResource = _mapper.Map<IEnumerable<EventDTO>> (events);
            return Ok (eventsResource);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMainPhoto(int id)
        {
            var photo = await _repository.GetMainPhoto(id);
            return Ok(photo);
        }

        [HttpPost("{id}")]
        public async Task<IActionResult> AddPhoto(int id, [FromForm] PhotoForCreationDTO photoForCreationDTO)
        {
            var singleEvent = await _repository.GetEvent(id);
            if(singleEvent == null)
                return BadRequest("Cannot upload photo for unexisting entity");

            var file = photoForCreationDTO.File;
            if (file.Length > _photoSettings.MaxBytes) return BadRequest ("Maximum file size exceeded");
            if (!_photoSettings.isSupported (file.FileName)) return BadRequest ("Invalid file type.");
            var uploadResult = new ImageUploadResult();

            if (file.Length > 0)
            {
                using (var stream = file.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(file.Name, stream)
                    };

                    uploadResult = _cloudinary.Upload(uploadParams);
                }
            }
            photoForCreationDTO.FileName = uploadResult.Uri.ToString();
            photoForCreationDTO.PublicId = uploadResult.PublicId;
            photoForCreationDTO.EventId = id;

            var photo = _mapper.Map<Photo>(photoForCreationDTO);
            
            singleEvent.Images.Add(photo);

            if(await _repository.SaveAllChanges()){
                return Ok();
            }

            return BadRequest("Could not add photo");
        }

        [HttpPost("setMain")]
        public async Task<IActionResult> SetMain(SetMainPhotoId setMainPhoto)
        {
            var photo = await _repository.GetPhoto(setMainPhoto.NewMainId);

            if(photo == null)
                return BadRequest("Photo does not exist");
            if(photo.IsMain)
                return BadRequest("Photo is already main photo");
            
            if(setMainPhoto.CurrentMainId != 0) {
                var currentMainPhoto = await _repository.GetMainPhoto(setMainPhoto.CurrentMainId);
                
                if(currentMainPhoto != null){
                    currentMainPhoto.IsMain = false;
                }
            }

            photo.IsMain = true;

            if(await _repository.SaveAllChanges())
                return Ok();
            return BadRequest("Could not set main photo");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(int id)
        {
            var photoToDelete = await _repository.GetPhoto(id);

            if(photoToDelete.IsMain)
                return BadRequest("You can't delete the main photo");

            if(photoToDelete.PublicId != null)
            {
                var deleteParams = new DeletionParams(photoToDelete.PublicId);

                var result = _cloudinary.Destroy(deleteParams);

                if (result.Result == "ok")
                {
                    _repository.Delete(photoToDelete);
                }
            }

            if (photoToDelete.PublicId == null)
            {
                _repository.Delete(photoToDelete);
            }

            if(await _repository.SaveAllChanges())
                return Ok();
            
            return BadRequest("Failed to delete photo");
        }
        [HttpGet("getEvent/{eventId}")]
        public async Task<IActionResult> GetSingleEvent(int eventId)
        {
            var singleEvent = await _repository.GetEvent(eventId);
            if (singleEvent == null) {
                return NotFound("Event does not exist");
            }
            var eventToReturn = _mapper.Map<EventDTO>(singleEvent);
            return Ok(eventToReturn);
        }

        private async void SendEmail(Message message)
        {
            var apiKey = _configuration.GetSection("SendGridApiKey").Value;
            var sendGridclient = new SendGridClient (apiKey);
            var from = new EmailAddress (message.FromEmail, message.FromName);
            var subject = message.Subject;
            var to = new EmailAddress (message.ToEmail, message.ToName);
            var htmlContent = message.HtmlContent;
            var msg = MailHelper.CreateSingleEmail (from, to, subject, null, message.HtmlContent);
            var response = await sendGridclient.SendEmailAsync (msg);
        }
    }
}