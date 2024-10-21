using Clinic.models;
using Npgsql;
using NpgsqlTypes;
using System.Data;

namespace Clinic.Data
{
    public class USER_PKG:AddDbContext
    {
        public NpgsqlConnection conn { set; get; }

        public NpgsqlCommand cmd { set; get; }
        public USER_PKG(IConfiguration config):base(config) {
            this.conn = new NpgsqlConnection();
            this.conn.ConnectionString = ConnectionString;
            this.cmd = new NpgsqlCommand();
        }

        public User FindUser(int id)
        {
            this.conn.Open();
            this.cmd.Connection = this.conn;
            this.cmd.CommandText = "select us.id,us.name,us.surname,us.email,us.role,us.private_number,us.category,us.photo,us.resume from public.users us where us.id=@userId";
            this.cmd.CommandType = CommandType.Text;
            this.cmd.Parameters.Add("@userId", NpgsqlDbType.Integer).Value = id;

            var reader = this.cmd.ExecuteReader();

            User findUser = null;

            if (reader.Read())
            {
                findUser = new User
                {
                    id = reader.GetInt32(0),
                    name = reader.GetString(1),
                    surname = reader.GetString(2),
                    email = reader.GetString(3),
                    role = reader.GetString(4),
                    private_number = reader.GetString(5),
                    category = reader.GetString(6),
                    photo = reader.GetString(7),
                    resume = reader.GetString(8)
                };
            }
            this.conn.Close();

            return findUser;

        }
    }
}
