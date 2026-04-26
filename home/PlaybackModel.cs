using StudyApp.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace StudyApp.ViewModels
{
    public class PlaybackModel
    {

        /// <summary>
        /// A list representing course resources.
        /// This can be used to display lecture thumbnails or playback content.
        /// </summary>



        public List<string> CameraList { get; set; } = new List<string>
        {
            "lec1.png",
            "lec2.png",
            "lec3.png",
            "lec4.png"
        };

        public List<ErrorModel> ErrorList { get; set; } = new List<ErrorModel>();



        /// <summary>
        /// A list of ErrorModel objects.
        /// Initializes the ErrorList with sample data.
        /// Initializes the ErrorList with sample data , like missing assignments, upcoming exams
        /// </summary>



        public PlaybackModel()
        {
            ErrorList.Add(new ErrorModel
            {
                Header = "Assignment missing",
                Description = "Missing submission of Assignment 2.",
                Address = "Math",
                Color = "Red"
            });

            ErrorList.Add(new ErrorModel
            {
                Header = "Exam ahead",
                Description = "Exam next week",
                Address = "Java programming",
                Color = "#FFA500"
            });

            ErrorList.Add(new ErrorModel
            {
                Header = "Workshop",
                Description = "Interview workshop On Friday",
                Address = "Main hall",
                Color = "#87CEEB"
            });
        }

    }
}
