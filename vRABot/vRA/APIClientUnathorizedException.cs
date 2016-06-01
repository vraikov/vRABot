using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace vRABot.vRA
{
    public class APIClientUnathorizedException : APIClientException
    {
        public APIClientUnathorizedException()
            : base() { }

        public APIClientUnathorizedException(string message)
            : base(message) { }

        public APIClientUnathorizedException(string message, Exception inner)
            : base(message, inner) { }
    }
}