"use client";

import qs from "query-string";
import { useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { animateScroll as scroll } from "react-scroll";

import {
  Pagination,
  PaginationContent,
  PaginationEllipsis,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
} from "@/components/ui/pagination";
import { calculatePages } from "@/lib/utils";

type Props = {
  totalCount: number;
  pageSize: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
};

export const CustomPagination = ({
  totalCount,
  pageSize,
  hasNextPage,
  hasPreviousPage,
}: Props) => {
  const searchParams = useSearchParams();
  const router = useRouter();

  const [currentPage, setCurrentPage] = useState(1);

  const handlePageChange = (page: number) => {
    const amenity = searchParams.get("amenity");

    const url = qs.stringifyUrl({
      url: "/",
      query: {
        amenity,
        page,
      },
    });

    router.push(url);

    scroll.scrollToTop({ duration: 500, smooth: "easeInOutQuad" });

    setCurrentPage(page);
  };

  const totalPages = Math.ceil((totalCount || 0) / (pageSize || 1));

  const pages = calculatePages(totalPages, currentPage);

  return (
    <Pagination>
      <PaginationContent>
        {hasPreviousPage && (
          <PaginationItem>
            <PaginationPrevious
              onClick={() => handlePageChange(currentPage - 1)}
            />
          </PaginationItem>
        )}

        {pages[0] > 1 && (
          <>
            <PaginationItem>
              <PaginationLink onClick={() => handlePageChange(1)}>
                1
              </PaginationLink>
            </PaginationItem>
            <PaginationItem>
              <PaginationEllipsis />
            </PaginationItem>
          </>
        )}

        {pages.map((page) => (
          <PaginationItem key={page}>
            <PaginationLink
              isActive={page === currentPage}
              onClick={() => handlePageChange(page)}>
              {page}
            </PaginationLink>
          </PaginationItem>
        ))}

        {pages[pages.length - 1] < totalPages && (
          <>
            <PaginationItem>
              <PaginationEllipsis />
            </PaginationItem>
            <PaginationItem>
              <PaginationLink onClick={() => handlePageChange(totalPages)}>
                {totalPages}
              </PaginationLink>
            </PaginationItem>
          </>
        )}

        {hasNextPage && (
          <PaginationItem>
            <PaginationNext onClick={() => handlePageChange(currentPage + 1)} />
          </PaginationItem>
        )}
      </PaginationContent>
    </Pagination>
  );
};
