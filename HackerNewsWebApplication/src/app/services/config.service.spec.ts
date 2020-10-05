
import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ConfigService } from './config.service';

describe('ConfigService', () => {
  beforeEach(() => TestBed.configureTestingModule({
    imports: [HttpClientTestingModule],
    providers: [ConfigService]
  }));

   it('should be created', () => {
    const service: ConfigService = TestBed.get(ConfigService);
    expect(service).toBeTruthy();
   });

   it('should have getData function', () => {
    const service: ConfigService = TestBed.get(ConfigService);
    expect(service.loadConfiguration).toBeTruthy();
   });
});
