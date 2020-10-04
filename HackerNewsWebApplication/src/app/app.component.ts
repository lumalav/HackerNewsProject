import { Component, ViewChild, OnInit, OnDestroy, ElementRef } from '@angular/core';
import { MatSort, MatPaginator, MatSnackBar, Sort } from '@angular/material';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/operator/concatMap';
import 'rxjs/add/observable/fromEvent';
import 'rxjs/add/operator/switchMap';
import { Subject } from 'rxjs/Subject';
import { takeUntil, mergeMap } from 'rxjs/operators';
import { FormGroup, FormControl } from '@angular/forms';
import { merge } from 'rxjs/internal/observable/merge';
import { timer } from 'rxjs/internal/observable/timer';
import { debounce, distinctUntilChanged } from 'rxjs/operators';
import { DataService } from './services/data.service';
import { ConfigService } from './services/config.service';
import { forkJoin } from 'rxjs';
import * as psl from 'psl';
import * as moment from 'moment';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit, OnDestroy {
  @ViewChild('searchElement') searchElement: ElementRef;

  @ViewChild('table') private table: ElementRef<any>;
  @ViewChild(MatPaginator) paginator: MatPaginator;

  constructor(public _dataService: DataService,
    public _config: ConfigService,
    private snackBar: MatSnackBar) { }

  destroy$: Subject<any>;
  destroy2$: Subject<any>;
  subject: Subject<any>;
  loadItemSubject: Subject<any>;
  queryParamsForm: FormGroup;
  loading: boolean;
  pageSize = 50;
  totalResults = 0;
  propertyNames: string[];
  columnNames: string[];
  columnNamesAndDisplayedNames: any;
  results: any[];
  resultsObj: any;
  errorMessage: string;
  dataSource = [];
  savedSort: Sort = null;
  query: any = {};

  ngOnInit(): void {
    this.columnNamesAndDisplayedNames = { 'Results': 'Results'};
    this.propertyNames = Object.keys(this.columnNamesAndDisplayedNames).slice();
    this.columnNames = this.propertyNames.slice();
    this.subject = new Subject();
    this.loadItemSubject = new Subject();
    this.subscribe();
    this.subscribeItemSubject();
    this.queryParamsForm = new FormGroup(this.getControls());
    this.hookInputEventHandlers(this.searchElement, 'Search');
    this.pageSize = this._config.pageSize;
    this.fetchData();
  }

  fetchData(): void {
    if (!this.queryParamsForm || !this.queryParamsForm.valid) {
      return;
    }

    this.query['pageIndex'] = this.paginator ? this.paginator.pageIndex : 0;
    this.query['pageSize'] = this.pageSize;
    this.loading = true;
    this.subject.next(this.query);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.destroy2$.next();
    this.destroy2$.complete();
    this.snackBar.dismiss();
    this.loading = false;
  }

  cancel(): void {
    if (this.loading) {
      this.destroy$.next();
      this.destroy$.complete();
      this.destroy2$.next();
      this.destroy2$.complete();
      this.loading = false;
      this.subscribe();
      this.subscribeItemSubject();
    } else {
      this.queryParamsForm = new FormGroup(this.getControls());
      delete this.query['Search'];
    }
  }

  private subscribe(): void {
    this.destroy$ = new Subject();
    this.subject.concatMap(i => this._dataService.getTopNews(i))
    .pipe(takeUntil(this.destroy$))
    .subscribe(result => {
      console.log('data arriving');
      this.prepareData(result);
      this.finishLoad();
    }, error => {
      this.snack(error);
      this.finishLoad();
      this.cancel();
    });
  }

  private subscribeItemSubject(): void {
    this.destroy2$ = new Subject();
    let items = null;
    this.loadItemSubject.concatMap(i => {
      items = i;
      return forkJoin(items.map(j => this._dataService.loadNews(j)));
    }).pipe(takeUntil(this.destroy2$))
    .subscribe(result => {
      result.forEach(item => {
        const r = <any>item;
        if (this.resultsObj.hasOwnProperty(r.id)) {
          Object.keys(r).forEach(i => {
            this.results[this.resultsObj[r.id]][i] = item[i];
          });
        }
      })
    }, error => {
      this.snack(error);
      this.finishLoad();
      this.cancel();
    });
  }

  private getControls(): any {
    return {
      Search: new FormControl('', [])
    };
  }

  prepareData(fetchResult: any): void {
    this.results = fetchResult.results;
    this.resultsObj = {};
    let c = 0;
    this.results.forEach(i => {
      this.resultsObj[i.id] = c++;
    })
    this.totalResults = fetchResult.total;
    console.log(`results: ${this.results.length}, totalResults: ${this.totalResults}`);
    this.dataSource = this.results;

    if (!this.results.length && this.totalResults) {
      this.paginator.firstPage();
    }

    this.loadItemSubject.next(this.results);
  }

  private snack(error: any, milliseconds?: number, color: string = 'red'): any {
    const snack = this.snackBar.open(error, color === 'red' ? 'OK' : null, {
      duration: milliseconds || null,
      panelClass: [`${color}-snackbar`]
    });

    return snack;
  }

  private finishLoad(): void {
    setTimeout(() => {
      this.loading = false;
    }, 100);
  }

  reload(): void {
    this.fetchData();
  }

  isBoolean(value: any): boolean {
    return typeof value === 'boolean';
  }

  isDataTypeColumn(value: any): boolean {
    return value && typeof value === 'object' && value.hasOwnProperty('Name');
  }

  onPageChange($event): void {
    this._config.pageSize = $event.pageSize;
    this.pageSize = this._config.pageSize;
    this.fetchData();
  }

  scrollTo(direction: string, pixels: number = null): void {
    if (pixels) {
      const current = this.table['_elementRef'].nativeElement.scrollTo;
      if (direction === 'top' && current === 0 || direction !== 'top' && current === this.table['_elementRef'].nativeElement.scrollHeight) {
        return;
      }
      this.table['_elementRef'].nativeElement.scrollTop = direction === 'top' ?
        this.table['_elementRef'].nativeElement.scrollTop - pixels :
        this.table['_elementRef'].nativeElement.scrollTop + pixels;
    } else {
      this.table['_elementRef'].nativeElement.scrollTop = direction === 'top' ? 0 : this.table['_elementRef'].nativeElement.scrollHeight;
    }
  }

  fetchWithDelay() {
    setTimeout(() => this.fetchData(), 1000);
  }

  private hookInputEventHandlers(element: any, property: string): void {
    const eventStream = Observable.fromEvent(element.nativeElement, 'keyup');
    const eventStreamBlur = Observable.fromEvent(element.nativeElement, 'blur');

    merge(eventStream, eventStreamBlur)
    .pipe(debounce(i => (<any>i).keyCode !== 13 ? timer(1000) : timer(0)))
    .pipe(distinctUntilChanged(null, i => (<any>i).target.value))
    .subscribe(i => {

      const value = ((<any>i).target.value || '').trim();

      if (!value) {
        if (this.query.hasOwnProperty(property)) {
          delete this.query[property];
          this.paginator.firstPage();
        }
      } else {
        if (this.query.hasOwnProperty(property) && value === this.query[property]) {
          return;
        }

        this.query[property] = value;
      }

      this.fetchData();
    });
  }

  controlHasValue(id: string): boolean {
    return this.queryParamsForm.controls[id].value;
  }

  clearValue(id: string): void {
    this.queryParamsForm.controls[id].setValue('');
    // delete this.query['Search'];
    // this.paginator.firstPage();
    // this.fetchData();
  }

  getBaseUrl(item: any): string {
    const urlObject = new URL(item.url);

    return urlObject.origin;
  }

  getHostName(item: any): string {
    let hostname;

    if (item.url.indexOf('//') > -1) {
        hostname = item.url.split('/')[2];
    } else {
        hostname = item.url.split('/')[0];
    }

    hostname = hostname.split(':')[0];
    hostname = hostname.split('?')[0];

    return psl.get(hostname);
  }

  timeSince(date): string {
    return moment(new Date(date * 1000)).fromNow();
  }
}
