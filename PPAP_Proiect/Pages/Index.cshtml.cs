using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using PPAP_Proiect.Data;
using PPAP_Proiect.Models;

public class IndexModel : PageModel
{
	private readonly DBConectare _db;

	public IndexModel(DBConectare db)
	{
		_db = db;
	}

	public Utilizator userCurent;
	public void OnGet(string status)
	{
		int? userId = HttpContext.Session.GetInt32("userId");
		
		ModelState.Remove("mesajSucces");
		
		if (userId != null)
		{
			FunctiiUtilizator helper = new FunctiiUtilizator(_db);
			userCurent = helper.GetUtilizatorSession(userId.Value);
		}

		if (status == "succes")
		{
			ModelState.AddModelError("mesajSucces", "Logat cu succes! Bine ai venit!");
		}
		if(status == "succes_edit")
		{
			ModelState.AddModelError("mesajSucces", "Cont editat cu succes!");
		}



	}
}
