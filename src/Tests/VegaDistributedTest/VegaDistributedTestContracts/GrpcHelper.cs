// <copyright file="GrpcHelper.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.DistributedTest
{
    using System.Collections.Generic;

    using DistTestCommonProto;
    using Google.Protobuf.Collections;

    /// <summary>
    /// The Grpc helper methods.
    /// </summary>
    public static class GrpcHelper
    {
        /// <summary>
        /// Get job parameters in dictionary format.
        /// </summary>
        /// <param name="repeatedField">the repeated field which contains the job parameters</param>
        /// <returns>dictionary of parameters</returns>
        public static Dictionary<string, string> GetJobParametersFromRepeatedField(RepeatedField<JobParameter> repeatedField)
        {
            var dictionary = new Dictionary<string, string>();
            foreach (var field in repeatedField)
            {
                dictionary.Add(field.Key, field.Value);
            }

            return dictionary;
        }

        /// <summary>
        /// Gets the job parameters from dictionary.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>repeated filed of job parameters</returns>
        public static RepeatedField<JobParameter> GetJobParametersFromDictionary(Dictionary<string, string> parameters)
        {
            var repeatedField = new RepeatedField<JobParameter>();

            foreach (var kvpair in parameters)
            {
                repeatedField.Add(new JobParameter
                {
                    Key = kvpair.Key,
                    Value = kvpair.Value,
                });
            }

            return repeatedField;
        }
    }
}
