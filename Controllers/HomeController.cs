using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TestEdison.Controllers
{
    public class HomeController : Controller
    {
        string[] arrVarFName = new string[] { "Иван", "Петр", "Сидр" };
        string[] arrVarLName = new string[] { "Иванов", "Петров", "Сидоров" };

        public static List<string> listTestableMedium;
        public static List<int> users;
        public static Dictionary<int, TestEdison.Models.Request> listHistoryTest;

        private List<IEnumerable<T>> VarName<T>(IEnumerable<IEnumerable<T>> sequences)
        {
            IEnumerable<IEnumerable<T>> emptyName = new[] { Enumerable.Empty<T>() };
            return sequences.Aggregate(
              emptyName,
              (accumulator, sequence) =>
                from accseq in accumulator
                from item in sequence
                select accseq.Concat(new[] { item })).ToList();
        }


        public ActionResult Index()
        {
            if (users == null)
                users = new List<int>();
            if (HttpContext.Session["user_id"] == null)
            {
                users.Add(users.Count());
                HttpContext.Session["user_id"] = users.Count();
            }
            else
            {
                if (users.IndexOf(Convert.ToInt32(HttpContext.Session["user_id"])) == -1)
                {
                    users.Add(Convert.ToInt32(HttpContext.Session["user_id"]));
                }
            }

            if (listTestableMedium == null)
            {
                //Список заявленных экстрасенсов
                List<IEnumerable<string>> listActualMedium = VarName(new[] { arrVarFName, arrVarLName });

                //Определим количество участников
                Random rnd = new Random();
                int cntMedium = rnd.Next(2, listActualMedium.Count() - 1);

                //Определяем имена участников
                listTestableMedium = new List<string>();
                while (listTestableMedium.Count() < cntMedium)
                {
                    int posMedium = rnd.Next(0, listActualMedium.Count() - 1);
                    string nameMedium = String.Format("{0} {1}",
                                                      listActualMedium[posMedium].ToArray()[0],
                                                      listActualMedium[posMedium].ToArray()[1]);
                    if (!listTestableMedium.Contains(nameMedium))
                    {
                        listTestableMedium.Add(nameMedium);
                    }
                }
            }

            ViewData["Mediums"] = listTestableMedium;
            ViewData["SesionID"] = HttpContext.Session["user_id"];
            ViewData["VarReq"] = "Medium";
            return View("Index");
        }

        public string History(int forWhom)
        {
            var listResult = listHistoryTest;
            if (forWhom == -1)
            {
                return string.Join("<br/>",
                    listHistoryTest.Where(x => x.Value.UserID == Convert.ToInt32(HttpContext.Session["user_id"]) &&
                                               x.Value.ReqValue != 0)
                                   .Select(x => x.Value.DateTime.ToString() + " Загадано число:" + x.Value.ReqValue));

            }
            else
            {
                return string.Join("<br/>",
                    listHistoryTest.Where(x => x.Value.ReqValue != 0)
                                   .Select(x => x.Value.DateTime.ToString() +
                                                " Загадано " + 
                                                ((x.Value.UserID == Convert.ToInt32(HttpContext.Session["user_id"])) ? "(Вами)" : "(другим тестировщиком)") +
                                                " число:" + x.Value.ReqValue +
                                                " Предположение экстрасенса:" + x.Value.MediumsResult[forWhom].MediumData));
            }
        }

        private List<int> StatMedium(bool byUserID = false)
        {
            List<int> resList = new List<int>();
            for (int i = 0; i < listTestableMedium.Count(); i++)
            {
                var statByMedium = (!byUserID ? listHistoryTest
                                              : listHistoryTest.Where(x => x.Value.UserID == Convert.ToInt32(HttpContext.Session["user_id"])))
                                  .Where(x=>x.Value.ReqValue != 0)
                                  .Select(x => x.Value.MediumsResult[i]);
                resList.Add((statByMedium == null) ? 0 : statByMedium.Select(x => x.ValidData).Sum());
            }

            return resList;
        }

        public ActionResult StartTest(string msg)
        {
            //Проверка инициализации истории
            if (listHistoryTest == null)
            {
                listHistoryTest = new Dictionary<int, Models.Request>();
            }

            //Ограничение бесплатного хостинга (не будем переполнять)
            if (listHistoryTest.Count() >= 10000)
            {
                listHistoryTest.Clear();
                users.Clear();
            }

            //Список медиумов
            ViewData["Mediums"] = listTestableMedium;

            //Список догадок медиумов           
            List<TestEdison.Models.MediumVar> mediumResult = new List<TestEdison.Models.MediumVar>();
            Random rnd = new Random();
            for (int i = 0; i < listTestableMedium.Count(); i++)
            {
                mediumResult.Add(new TestEdison.Models.MediumVar()
                                {
                                    MediumData = rnd.Next(10, 99),
                                    ValidData = 0
                                });
            }
            ViewData["Results"] = mediumResult;

            //Список текущих достоверностей медиумов для пользователя

            int keyRec = (listHistoryTest.Count() > 0) ? listHistoryTest.Keys.Max() : 0;

            Models.Request mReq = new Models.Request()
                                                    {
                                                        UserID = Convert.ToInt32(HttpContext.Session["user_id"]),
                                                        ReqValue = 0,
                                                        MediumsResult = mediumResult,
                                                        DateTime = DateTime.Now
                                                    };

            listHistoryTest.Add(keyRec + 1, mReq);
            ViewData["KeyReq"] = keyRec + 1;
            ViewData["VarReq"] = "Medium";
            ViewData["Stat"] = StatMedium(true);
            ViewData["StatFull"] = StatMedium();

            if (msg != null)
            {
                ViewData["ValidError"] = msg;
            }
            return PartialView("_TestedMedium", mReq);
        }

        public ActionResult TestMediumResult(int KeyRec, int ReqValue)
        {
            Models.Request mReq = null;
            if (listHistoryTest.Keys.Contains(KeyRec))
            {
                mReq = listHistoryTest[KeyRec];
            }
            //Список медиумов
            ViewData["Mediums"] = listTestableMedium;

            if (ReqValue < 10 || ReqValue > 99)
            {
                ViewData["ValidError"] = "* - Введенное значение не является двузначным числом!";
                ViewData["VarReq"] = "Medium";
                return PartialView("_TestedMedium", mReq);
            }
            else
            {
                if (mReq != null)
                {
                    mReq.ReqValue = ReqValue;
                    mReq.MediumsResult.ForEach(x => x.ValidData = (x.MediumData == mReq.ReqValue) ? 1 : -1);
                    ViewData["Stat"] = StatMedium(true);
                    ViewData["StatFull"] = StatMedium();

                    return PartialView("_TestedMedium", mReq); 
                }
                else
                {
                    return StartTest("Просим прощения, экстрасенсы передумали и дали другой ответ!");
                }
            }
        }
    }
}
