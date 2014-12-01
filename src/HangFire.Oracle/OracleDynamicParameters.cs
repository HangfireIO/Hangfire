using System.Collections.Generic;
using System.Data;
using Dapper;
using Oracle.ManagedDataAccess.Client;

namespace Hangfire.Oracle {
    public class OracleDynamicParameters : SqlMapper.IDynamicParameters {
        private readonly DynamicParameters _dynamicParameters = new DynamicParameters();
        private readonly List<OracleParameter> _oracleParameters = new List<OracleParameter>();

        public void Add(string name, OracleDbType oracleDbType, ParameterDirection direction, object value = null, int? size = null) {
            var oracleParameter = size.HasValue ? 
                new OracleParameter(name, oracleDbType, size.Value, value, direction) : 
                new OracleParameter(name, oracleDbType, value, direction);
            _oracleParameters.Add(oracleParameter);
        }

        public void Add(string name, OracleDbType oracleDbType, ParameterDirection direction) {
            var oracleParameter = new OracleParameter(name, oracleDbType, direction);
            _oracleParameters.Add(oracleParameter);
        }

        public void AddParameters(IDbCommand command, SqlMapper.Identity identity) {
            ((SqlMapper.IDynamicParameters)_dynamicParameters).AddParameters(command, identity);
            var oracleCommand = command as OracleCommand;
            if (oracleCommand != null) {
                oracleCommand.Parameters.AddRange(_oracleParameters.ToArray());
            }
        }
    }
}