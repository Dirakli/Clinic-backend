using Clinic.models;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Data;
using System.Xml.Linq;

namespace Clinic.Data
{
    public class AUTH_PKG: AddDbContext
    {
        public NpgsqlConnection conn { set; get; }

        public NpgsqlCommand cmd { set; get; }
        public AUTH_PKG(IConfiguration config):base(config) {
            this.conn = new NpgsqlConnection();
            this.conn.ConnectionString = ConnectionString;
            this.cmd = new NpgsqlCommand();
        }


        public User AddUser(User user)
        {
            this.conn.Open();
            this.cmd.Connection = this.conn;
            this.cmd.CommandText = "INSERT INTO public.users (name, surname, email, password, role, private_number, category, photo, resume) " +
                "VALUES (@name, @surname, @email, @password, @role, @private_number, @category, @photo, @resume)" +
                "RETURNING id, name, surname, email, role, private_number, category, photo, resume";
            this.cmd.CommandType = CommandType.Text;
            this.cmd.Parameters.Add("@name", NpgsqlDbType.Varchar).Value = user.name;
            this.cmd.Parameters.Add("@surname", NpgsqlDbType.Varchar).Value = user.surname;
            this.cmd.Parameters.Add("@email", NpgsqlDbType.Varchar).Value = user.email;
            this.cmd.Parameters.Add("@password", NpgsqlDbType.Varchar).Value = user.password;
            this.cmd.Parameters.Add("@role", NpgsqlDbType.Varchar).Value = user.role;
            this.cmd.Parameters.Add("@private_number", NpgsqlDbType.Varchar).Value = user.private_number;
            this.cmd.Parameters.Add("@category", NpgsqlDbType.Varchar).Value = user.category;
            this.cmd.Parameters.Add("@photo", NpgsqlDbType.Varchar).Value = user.photo;
            this.cmd.Parameters.Add("@resume", NpgsqlDbType.Varchar).Value = user.resume;
            var reader = this.cmd.ExecuteReader();

            User createdUser = null;

            if (reader.Read())
            {
                createdUser = new User
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
                this.conn.Close();
                return createdUser;
            }

            this.conn.Close();

            return null; 

        }

        public User FindUser(string email)
        {
            this.conn.Open();
            this.cmd.Connection = this.conn;
            this.cmd.CommandText = "select us.id,us.name,us.surname,us.email,us.role,us.password,us.private_number,us.category,us.photo,us.resume from public.users us where email=@email";
            this.cmd.CommandType = CommandType.Text;
            this.cmd.Parameters.Add("@email", NpgsqlDbType.Varchar).Value = email;

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
                    password=reader.GetString(5),
                    private_number = reader.GetString(6),
                    category = reader.GetString(7),
                    photo = reader.GetString(8),
                    resume = reader.GetString(9)
                };
            }
            this.conn.Close();

            return findUser;

        }

        public void ChangePassword(string email,string password)
        {
            this.conn.Open();
            this.cmd.Connection = this.conn;
            this.cmd.CommandText = "UPDATE public.users SET password = @Password WHERE email = @Email";
            this.cmd.CommandType = CommandType.Text;
            this.cmd.Parameters.Add("@email", NpgsqlDbType.Varchar).Value = email;
            this.cmd.Parameters.Add("@Password", NpgsqlDbType.Varchar).Value = password;
            this.cmd.ExecuteNonQuery();
            this.conn.Close();

        } 

    }
}
