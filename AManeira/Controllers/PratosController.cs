﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AManeira.Data;
using AManeira.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Data;
//using System.Web.Mvc;

namespace AManeira.Controllers
{
    public class PratosController : Controller
    {
        /// <summary>
        /// cria uma referência à base de dados do projeto
        /// </summary>
        private readonly ApplicationDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        /// <summary>
        /// esta variável contém os dados do servidor ASP .NET
        /// </summary>
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PratosController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
        }

        // GET: Pratos
        public async Task<IActionResult> Index()
        {
            return View(await _context.Pratos.ToListAsync());
        }

        // GET: Pratos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Pratos == null)
            {
                return RedirectToAction("Index");
            }

            var pratos = await _context.Pratos
                .Include(l => l.ListaFotos)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (pratos == null)
            {
                return RedirectToAction("Index");
            }

            return View(pratos);
        }
        [Authorize(Roles = "Admin")]
        // GET: Pratos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Pratos/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nome,Preco,Foto,Descricao,NumStock")] Pratos pratos)
        {
            if (ModelState.IsValid)
            {
                _context.Add(pratos);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(pratos);
        }

        // GET: Pratos/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Pratos == null)
            {
                return RedirectToAction("Index");
            }

            var pratos = await _context.Pratos.FindAsync(id);
            if (pratos == null)
            {
                return RedirectToAction("Index");
            }
            return View(pratos);
        }

        // POST: Pratos/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nome,Preco,Foto,Descricao,NumStock")] Pratos pratos)
        {
            if (id != pratos.Id)
            {
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(pratos);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PratosExists(pratos.Id))
                    {
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(pratos);
        }

        // GET: Pratos/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Pratos == null)
            {
                return RedirectToAction("Index");
            }

            var pratos = await _context.Pratos
                .FirstOrDefaultAsync(m => m.Id == id);
            if (pratos == null)
            {
                return RedirectToAction("Index");
            }

            return View(pratos);
        }

        // POST: Pratos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Pratos == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Pratos'  is null.");
            }
            var pratos = await _context.Pratos.FindAsync(id);
            if (pratos != null)
            {
                _context.Pratos.Remove(pratos);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PratosExists(int id)
        {
            return _context.Pratos.Any(e => e.Id == id);
        }

        [HttpPost]
        public async Task<JsonResult> AddToEncomenda(string idAux)
        {
            int id = Convert.ToInt32(idAux);

            //ir buscar o prato correspondente ao 'id'
            var prato = await _context.Pratos
               .Where(s => s.Id == id)
               .FirstOrDefaultAsync();

            //ir buscar o utilizador autenticado
            var user = _userManager.GetUserId(User);

            //procurar na tabela dos clientes pelo utilizador autenticado
            var cliente = await _context.Clientes
               .Where(s => s.UserID == user)
               .FirstOrDefaultAsync();

            // verificar em que encomenda por o prato
            var encomenda = await _context.Encomendas
               .Where(s => s.ClienteFK == cliente.ID && s.IsActive == true)
               .FirstOrDefaultAsync();
            //criar encomenda se não existe encomenda ativa relacionada ao cliente
            if (encomenda == null)
            {
                encomenda = new Encomendas { PrecoTotal = 0, ClienteFK = cliente.ID, IsActive = true };
                // associar o Prato à Encomenda
                var listaPrato = new List<EncomendasPratos>();
                listaPrato.Add(new EncomendasPratos { Prato = prato, Encomenda = encomenda, Quantidade = 1 });
                encomenda.ListaEncomendasPratos = listaPrato;
                // adicionar a encomenda à BD
                _context.Encomendas.Add(encomenda);
                await _context.SaveChangesAsync();
                //devolver caso de sucesso em Json
                return Json(new { id = "1" });
            }

            /* se chego aqui, a Encomenda já existia...
             */

            //var listaPrato2 = new List<EncomendasPratos>();
            //listaPrato2.Add(new EncomendasPratos { PratoFK = id, Encomenda = encomenda });
            //encomenda.ListaEncomendasPratos = listaPrato2;

            //ver se o prato já foi adicionado à encomenda
            //EncomendasPratos result = new()
            //EncomendasPratos result = _context.Find(encomenda.ListaEncomendasPratos.Where(t => t.PratoFK == id).FirstOrDefault());
            var lista = encomenda.ListaEncomendasPratos;
            var listaRegisto = lista.FirstOrDefault( e=> e.PratoFK == id);
            
            // se não foi
            if (listaRegisto == null)
            {
                //var pratoAdd = new EncomendasPratos { Prato = prato, Encomenda = encomenda, Quantidade = 1 };
                //encomenda.ListaEncomendasPratos.Add( pratoAdd);
                encomenda.PrecoTotal = 0;
                // atualizar os dados na BD
                _context.Entry(encomenda).State = EntityState.Modified;
                //_context.Encomendas.Update(encomenda);
                await _context.SaveChangesAsync();
                //devolver caso de sucesso em Json
                return Json(new { id = "1" });
            }
            //se já foi, ir buscar a quantidade e incrementar
            var qnt = listaRegisto.Quantidade;
            qnt++;
            encomenda.ListaEncomendasPratos.Add(new EncomendasPratos { Prato = prato, Encomenda = encomenda, Quantidade = qnt });

            // atualizar os dados na BD
            _context.Update(encomenda);
            await _context.SaveChangesAsync();
            //devolver caso de sucesso em Json
            return Json(new { id = "1" });
        }
    }

}

