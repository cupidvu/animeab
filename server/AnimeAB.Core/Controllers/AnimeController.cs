﻿using AnimeAB.Core.Validator.Filter;
using AnimeAB.Reponsitories.Domain;
using AnimeAB.Reponsitories.DTO;
using AnimeAB.Reponsitories.Entities;
using AnimeAB.Reponsitories.Interface;
using AnimeAB.Reponsitories.Utils;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

namespace AnimeAB.Core.Controllers
{
    [Route("anime/movies")]
    [Authorize(Policy = RoleSchemes.Admin, AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public class AnimeController : Controller
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IWebHostEnvironment _enviroment;
        private readonly IMapper _mapper;

        public AnimeController(IUnitOfWork unitOfWork, IWebHostEnvironment enviroment, IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            _enviroment = enviroment;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var collections = await unitOfWork.CollectionEntity.GetCollectionsAsync();
            var categories = await unitOfWork.CategoriesEntity.GetCategoriesAsync();
            ViewData["Collections"] = collections;
            ViewData["Categories"] = categories;
            return View();
        }

        [HttpPost]
        [Route("all")]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> GetAnimes([FromBody]AnimeDtoFilter filter)
        {
            var list = await unitOfWork.AnimeEntity.GetAnimesAsync();

            if(!string.IsNullOrWhiteSpace(filter.Category) && filter.Category != "all")
            {
                list = list.Where(x => x.CategoryKey.Equals(filter.Category)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(filter.Collection) && filter.Collection != "all")
            {
                list = list.Where(x => x.CollectionId.Equals(filter.Collection)).ToList();
            }

            if (filter.Status > 0)
            {
                list = list.Where(x => x.IsStatus.Equals(filter.Status)).ToList();
            }

            if (filter.Time > 0)
            {
                if(filter.Time == 1)
                {
                    list = list.OrderBy(x => x.DateRelease).ToList();
                }
                else
                {
                    list = list.OrderByDescending(x => x.DateRelease).ToList();
                }
            }

            return Ok(new { data = list });
        }

        [HttpPost]
        public async Task<IActionResult> Create(AnimeDto anime)
        {
            try
            {
                anime.FacebookUrl = "https://animeab.tk/" + anime.Key;
                if (ModelState.IsValid)
                {
                    if (anime.FileUpload == null && string.IsNullOrWhiteSpace(anime.Image))
                        return BadRequest("Bạn cần upload ảnh hoặc thêm url ảnh");

                    Animes item = _mapper.Map<Animes>(anime);

                    if (anime.FileUpload != null)
                    {
                        string uploads = Path.Combine(_enviroment.WebRootPath, $"image");
                        string filePath = Path.Combine(uploads, anime.FileUpload.FileName);

                        using (Stream fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await anime.FileUpload.CopyToAsync(fileStream);
                        }
                        using (FileStream fs = new FileStream(filePath, FileMode.Open))
                        {
                            item.FileName = anime.FileUpload.FileName;
                            var result = await unitOfWork.AnimeEntity.CreateAnimeAsync(item, fs);

                            fs.Close();
                            System.IO.File.Delete(filePath);

                            if (!result.Success) return BadRequest(result.Message);
                        }
                    }
                    else
                    {
                        var result = await unitOfWork.AnimeEntity.CreateAnimeAsync(item, null);
                        if (!result.Success)
                        {
                            return BadRequest(result.Message);
                        }
                    }

                    return NoContent();
                }
                else
                {
                    return BadRequest();
                }
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        [Route("{key}")]
        public async Task<IActionResult> Edit(AnimeDto animeDto, string key)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    AnimesDomain item = _mapper.Map<AnimesDomain>(animeDto);
                    if (animeDto.FileUpload != null)
                    {
                        if (animeDto.FileUpload.Length == 0) return BadRequest();

                        string uploads = Path.Combine(_enviroment.WebRootPath, $"image");
                        string filePath = Path.Combine(uploads, animeDto.FileUpload.FileName);

                        using (Stream fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await animeDto.FileUpload.CopyToAsync(fileStream);
                        }

                        using (FileStream fs = new FileStream(filePath, FileMode.Open))
                        {
                            item.FileName = animeDto.FileUpload.FileName;
                            var result = await unitOfWork.AnimeEntity.UpdateAnimeAsync(item, fs);

                            fs.Close();

                            System.IO.File.Delete(filePath);

                            if (!result.Success) return BadRequest(result.Message);
                            return Ok(result.Data);
                        }
                    }
                    else
                    {
                        var result = await unitOfWork.AnimeEntity.UpdateAnimeAsync(item, null);
                        if(!result.Success) return BadRequest(result.Message);
                        return Ok(result.Data);
                    }
                }
                else
                {
                    var errorResponse = ValidationHanlder.GetErrors(ModelState);
                    return BadRequest(errorResponse.Errors);
                }
            }
            catch
            {
                return BadRequest();
            }
        }

        [HttpDelete]
        [Route("{key}")]
        public async Task<IActionResult> Delete(string key)
        {
            var result = await unitOfWork.AnimeEntity.DeleteAnimeAsync(key);
            if (!result.Success) return NotFound();
            return NoContent();
        }

        [HttpPost]
        [Route("{key}/banner")]
        public async Task<IActionResult> UpdateBanner(string key, [FromForm]IFormFile file)
        {
            if (file == null) return BadRequest();

            string uploads = Path.Combine(_enviroment.WebRootPath, $"image");
            string filePath = Path.Combine(uploads, file.FileName);

            using (Stream fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            {
                var result = await unitOfWork.AnimeEntity.UpdateBannerAsync(key, file.FileName, fs);

                fs.Close();

                System.IO.File.Delete(filePath);

                if (!result.Success) return BadRequest(result.Message);

                return Ok(result.Data);
            }
        }

        [HttpGet]
        [Route("{key}/banner")]
        public async Task<IActionResult> DestroyBanner(string key)
        {
            var result = await unitOfWork.AnimeEntity.DestroyBannerAsync(key);
            if (!result.Success) return BadRequest(result.Message);
            return NoContent();
        }
    }
    public class AnimeDto
    {
        public string Key { get; set; }
        public string Image { get; set; }
        public IFormFile FileUpload { get; set; }
        public string Title { get; set; }
        public string TitleVie { get; set; }
        public string Description { get; set; }
        public string Trainer { get; set; }
        public int Episode { get; set; }
        public int MovieDuration { get; set; }
        public string CollectionId { get; set; }
        public string CategoryKey { get; set; }
        public string Type { get; set; }
        public string FacebookUrl { get; set; }
    }
}
