using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Issues.Relations;
using NzbDrone.Http.REST.Attributes;
using Panelarr.Http;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.Series
{
    public class SeriesRelationResource : RestResource
    {
        public int SeriesId { get; set; }
        public int RelatedSeriesId { get; set; }
        public SeriesRelationType RelationType { get; set; }
    }

    [V1ApiController("seriesrelation")]
    public class SeriesRelationController : RestController<SeriesRelationResource>
    {
        private readonly ISeriesRelationService _relationService;

        public SeriesRelationController(ISeriesRelationService relationService)
        {
            _relationService = relationService;
        }

        protected override SeriesRelationResource GetResourceById(int id)
        {
            return ToResource(_relationService.Get(id));
        }

        [HttpGet]
        public List<SeriesRelationResource> GetRelations(int seriesId)
        {
            return _relationService.GetBySeriesId(seriesId)
                .Select(ToResource)
                .ToList();
        }

        [RestPostById]
        public ActionResult<SeriesRelationResource> AddRelation([FromBody] SeriesRelationResource resource)
        {
            try
            {
                var relation = _relationService.Add(new SeriesRelation
                {
                    SeriesId = resource.SeriesId,
                    RelatedSeriesId = resource.RelatedSeriesId,
                    RelationType = resource.RelationType
                });

                return Created(relation.Id);
            }
            catch (ArgumentException ex)
            {
                throw new BadRequestException(ex.Message);
            }
        }

        [RestDeleteById]
        public void DeleteRelation(int id)
        {
            _relationService.Delete(id);
        }

        private static SeriesRelationResource ToResource(SeriesRelation relation)
        {
            return new SeriesRelationResource
            {
                Id = relation.Id,
                SeriesId = relation.SeriesId,
                RelatedSeriesId = relation.RelatedSeriesId,
                RelationType = relation.RelationType
            };
        }
    }
}
