﻿using System.Diagnostics;
using Basic.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MR.EntityFrameworkCore.KeysetPagination;

namespace Basic.Pages
{
	public class Example2Model : PageModel
	{
		private readonly AppDbContext _dbContext;

		public Example2Model(
			AppDbContext dbContext)
		{
			_dbContext = dbContext;
		}

		public int Count { get; set; }

		public List<User> Users { get; set; }

		public bool HasPrevious { get; set; }

		public bool HasNext { get; set; }

		public string Elapsed { get; set; }

		public string ElapsedTotal { get; set; }

		public async Task OnGet(int? after, int? before, bool first = false, bool last = false)
		{
			var size = 20;

			var keysetBuilderAction = (KeysetPaginationBuilder<User> b) =>
			{
				// It kind of doesn't make sense to add the Id here since Created will be unique, but this is just a sample.
				b.Descending(x => x.Created).Ascending(x => x.Id);
			};

			var sw = Stopwatch.StartNew();

			var query = _dbContext.Users.AsQueryable();
			Count = await query.CountAsync();
			KeysetPaginationContext<User> keysetContext;
			if (first)
			{
				keysetContext = query.KeysetPaginate(keysetBuilderAction, KeysetPaginationDirection.Forward);
			}
			else if (last)
			{
				keysetContext = query.KeysetPaginate(keysetBuilderAction, KeysetPaginationDirection.Backward);
			}
			else if (after != null)
			{
				var reference = await _dbContext.Users.FindAsync(after.Value);
				keysetContext = query.KeysetPaginate(keysetBuilderAction, KeysetPaginationDirection.Forward, reference);
			}
			else if (before != null)
			{
				var reference = await _dbContext.Users.FindAsync(before.Value);
				keysetContext = query.KeysetPaginate(keysetBuilderAction, KeysetPaginationDirection.Backward, reference);
			}
			else
			{
				keysetContext = query.KeysetPaginate(keysetBuilderAction);
			}

			Users = await keysetContext.Query
				.Take(size)
				.ToListAsync();

			keysetContext.EnsureCorrectOrder(Users);

			Elapsed = sw.ElapsedMilliseconds.ToString();

			HasPrevious = await keysetContext.HasPreviousAsync(Users);
			HasNext = await keysetContext.HasNextAsync(Users);

			ElapsedTotal = sw.ElapsedMilliseconds.ToString();
		}

#pragma warning disable IDE0051
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#nullable enable
		private void TestingTheAnalyzer()
		{
			// ===
			// Testing that the analyzer properly detects the following cases.
			// Removing the suppression should reveal errors on HEREs.

			// Uncomment the following pragma to see the analyzer reported diags.
#pragma warning disable KeysetPagination1000 // Keyset contains a nullable property

			var analyzerTestKeysetBuilderAction = (KeysetPaginationBuilder<User> b) =>
			{
				//                HERE
				b.Descending(x => x.NullableDate).Ascending(x => x.Id);
			};

			_dbContext.Users.KeysetPaginate(
				//                     HERE
				b => b.Descending(x => x.NullableDate).Ascending(x => x.Id));

			_dbContext.Users.KeysetPaginateQuery(
				//                    HERE
				b => b.Ascending(x => x.NullableDate));

			_dbContext.Users.KeysetPaginateQuery(
				//                    HERE (CS8602 warning)
				b => b.Ascending(x => x.NullableDetails.Id));

#pragma warning restore KeysetPagination1000 // Keyset contains a nullable property

			// ===
		}
#nullable disable
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore IDE0051
	}
}
