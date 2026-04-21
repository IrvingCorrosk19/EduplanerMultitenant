using System;
using System.Collections.Generic;

namespace SchoolManager.Models.ViewModels
{
    public class SubjectAssignmentViewModel
    {
        public Guid Id { get; set; }

        public Guid SpecialtyId { get; set; }

        public Guid AreaId { get; set; }

        public Guid SubjectId { get; set; }

        public Guid GradeLevelId { get; set; }

        public Guid GroupId { get; set; }

     

       


        // Propiedades relacionadas a las entidades para mostrar en la vista
        public string SpecialtyName { get; set; }
        public string AreaName { get; set; }
        public string SubjectName { get; set; }
        public string GradeLevelName { get; set; }
        public string GroupName { get; set; }

        public string Status { get; set; }




    }

   
}
